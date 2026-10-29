using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Event;

namespace Terp.Akka.Entities;

/// <summary>
/// A parent actor that routes messages to per-entity child actors by entity identity.
/// Entities are spawned lazily on first message and passivated after being idle.
/// </summary>
/// <remarks>
/// <para>
/// Uses Akka's own <see cref="Passivate"/> message: an entity requests passivation by sending
/// <c>Context.Parent.Tell(new Passivate(PoisonPill.Instance))</c>. The region buffers messages
/// for that entity, sends it the <see cref="Passivate.StopMessage"/>, and waits for
/// <see cref="Terminated"/>. This is the same protocol as Akka Cluster Sharding, so entity
/// actors work without changes when migrating to <c>WithShardRegion</c>.
/// </para>
/// <para>
/// Uses Akka's own <see cref="IMessageExtractor"/> for message routing, so the same extractor
/// works with both local entity regions and cluster sharding.
/// </para>
/// <para>
/// Idle entities are passivated automatically after <see cref="EntityRegionOptions.PassivationTimeout"/>.
/// </para>
/// <para>
/// Entities that terminate unexpectedly (without passivation) more than
/// <see cref="EntityRegionOptions.MaxRestarts"/> times within
/// <see cref="EntityRegionOptions.RestartWindow"/> are stopped permanently.
/// </para>
/// </remarks>
public sealed class EntityRegionActor : ReceiveActor, IWithTimers
{
    private const string PassivationTimerKey = "passivation-tick";

    private sealed record PassivationTick;
    private sealed record EntityState(IActorRef Ref, DateTimeOffset LastActivity);

    internal sealed record StartDrainCommand
    {
        internal static readonly StartDrainCommand Instance = new();
    }

    private readonly Func<IActorContext, string, IActorRef> _entityFactory;
    private readonly IMessageExtractor _extractor;
    private readonly EntityRegionOptions _options;
    private readonly string _regionTag;
    private readonly ILoggingAdapter _log;
    private readonly TimeProvider _timeProvider;

    private readonly Dictionary<string, EntityState> _entities = [];
    private readonly HashSet<string> _passivating = [];
    private readonly Dictionary<string, Queue<(object Message, IActorRef Sender)>> _buffers = [];
    private readonly Dictionary<string, List<DateTimeOffset>> _restartTimestamps = [];
    private readonly HashSet<string> _permanentlyStopped = [];

    /// <inheritdoc />
    public ITimerScheduler Timers { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityRegionActor"/> class.
    /// </summary>
    /// <param name="entityFactory">Creates a child actor for the given entity identity.</param>
    /// <param name="extractor">Extracts entity identity and inner message.</param>
    /// <param name="options">Region configuration.</param>
    /// <param name="regionTag">Tag value for metrics, typically the registry key type name.</param>
    /// <param name="timeProvider">Time source, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entityFactory"/>, <paramref name="extractor"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public EntityRegionActor(
        Func<IActorContext, string, IActorRef> entityFactory,
        IMessageExtractor extractor,
        EntityRegionOptions options,
        string regionTag,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(entityFactory);
        ArgumentNullException.ThrowIfNull(extractor);
        ArgumentNullException.ThrowIfNull(options);

        _entityFactory = entityFactory;
        _extractor = extractor;
        _options = options;
        _regionTag = regionTag;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _log = Context.GetLogger();

        Active();
    }

    /// <inheritdoc />
    protected override void PreStart()
    {
        Timers.StartPeriodicTimer(PassivationTimerKey, new PassivationTick(), _options.PassivationInterval);
    }

    private void Active()
    {
        Receive<PassivationTick>(_ => RunPassivation());
        Receive<StartDrainCommand>(_ => StartDrain());
        Receive<Passivate>(HandlePassivateRequest);
        Receive<Terminated>(HandleTerminated);
        ReceiveAny(HandleMessage);
    }

    private void Draining()
    {
        Receive<PassivationTick>(_ => { });
        Receive<Passivate>(_ => { });
        Receive<Terminated>(msg =>
        {
            RemoveEntity(msg.ActorRef);
            if (_entities.Count == 0)
            {
                Context.Stop(Self);
            }
        });
        ReceiveAny(msg => Context.System.DeadLetters.Tell(msg));
    }

    private void HandleMessage(object message)
    {
        var entityId = _extractor.EntityId(message);
        if (entityId is null)
        {
            Unhandled(message);
            return;
        }

        var entityMessage = _extractor.EntityMessage(message) ?? message;

        if (_permanentlyStopped.Contains(entityId))
        {
            _log.Warning("Entity '{0}' in region '{1}' is permanently stopped. Message sent to DeadLetters.", entityId, _regionTag);
            Context.System.DeadLetters.Tell(entityMessage);
            return;
        }

        if (_passivating.Contains(entityId))
        {
            BufferMessage(entityId, entityMessage, Sender);
            return;
        }

        var entity = GetOrSpawn(entityId);
        if (entity is null)
        {
            return;
        }

        entity.Tell(entityMessage, Sender);
        _entities[entityId] = _entities[entityId] with { LastActivity = _timeProvider.GetUtcNow() };
    }

    private void HandlePassivateRequest(Passivate passivate)
    {
        var entityId = FindEntityId(Sender);
        if (entityId is null)
        {
            _log.Warning("Received Passivate from unknown actor {0} in region '{1}'.", Sender, _regionTag);
            return;
        }

        if (_passivating.Contains(entityId))
        {
            return;
        }

        _passivating.Add(entityId);
        Sender.Tell(passivate.StopMessage);
    }

    private IActorRef? GetOrSpawn(string entityId)
    {
        if (_entities.TryGetValue(entityId, out var state))
        {
            return state.Ref;
        }

        if (_options.MaxEntities > 0 && _entities.Count >= _options.MaxEntities)
        {
            _log.Warning("Entity limit ({0}) reached in region '{1}'. Message for '{2}' sent to DeadLetters.",
                _options.MaxEntities, _regionTag, entityId);
            Context.System.DeadLetters.Tell(Sender);
            return null;
        }

        var actorRef = _entityFactory(Context, entityId);
        Context.Watch(actorRef);
        _entities[entityId] = new EntityState(actorRef, _timeProvider.GetUtcNow());
        EntityRegionMetrics.ActiveEntities.Add(1, new KeyValuePair<string, object?>("region", _regionTag));
        return actorRef;
    }

    private void RunPassivation()
    {
        var now = _timeProvider.GetUtcNow();
        var timeout = _options.PassivationTimeout;

        foreach (var (entityId, state) in _entities)
        {
            if (_passivating.Contains(entityId))
            {
                continue;
            }

            if (now - state.LastActivity <= timeout)
            {
                continue;
            }

            _passivating.Add(entityId);
            state.Ref.Tell(PoisonPill.Instance);
        }
    }

    private void HandleTerminated(Terminated terminated)
    {
        var entityId = FindEntityId(terminated.ActorRef);
        if (entityId is null)
        {
            return;
        }

        var wasPassivating = _passivating.Remove(entityId);
        RemoveEntity(terminated.ActorRef);

        if (wasPassivating)
        {
            EntityRegionMetrics.Passivations.Add(1, new KeyValuePair<string, object?>("region", _regionTag));
            ReplayBuffer(entityId);
            return;
        }

        EntityRegionMetrics.Restarts.Add(1, new KeyValuePair<string, object?>("region", _regionTag));
        RecordRestart(entityId);

        if (IsRestartLimitExceeded(entityId))
        {
            _permanentlyStopped.Add(entityId);
            _restartTimestamps.Remove(entityId);
            _log.Error("Entity '{0}' in region '{1}' exceeded restart limit ({2} in {3}). Permanently stopped.",
                entityId, _regionTag, _options.MaxRestarts, _options.RestartWindow);
            DiscardBuffer(entityId);
            return;
        }

        ReplayBuffer(entityId);
    }

    private void BufferMessage(string entityId, object message, IActorRef sender)
    {
        if (!_buffers.TryGetValue(entityId, out var queue))
        {
            queue = new Queue<(object, IActorRef)>();
            _buffers[entityId] = queue;
        }

        if (queue.Count >= _options.MaxBufferedMessagesPerEntity)
        {
            _log.Warning("Buffer overflow for entity '{0}' in region '{1}'. Message sent to DeadLetters.", entityId, _regionTag);
            Context.System.DeadLetters.Tell(message);
            return;
        }

        queue.Enqueue((message, sender));
        EntityRegionMetrics.MessagesBuffered.Add(1, new KeyValuePair<string, object?>("region", _regionTag));
    }

    private void ReplayBuffer(string entityId)
    {
        if (!_buffers.TryGetValue(entityId, out var queue) || queue.Count == 0)
        {
            _buffers.Remove(entityId);
            return;
        }

        var entity = GetOrSpawn(entityId);
        if (entity is null)
        {
            DiscardBuffer(entityId);
            return;
        }

        while (queue.Count > 0)
        {
            var (message, sender) = queue.Dequeue();
            entity.Tell(message, sender);
        }

        _buffers.Remove(entityId);
        _entities[entityId] = _entities[entityId] with { LastActivity = _timeProvider.GetUtcNow() };
    }

    private void DiscardBuffer(string entityId)
    {
        if (_buffers.TryGetValue(entityId, out var queue))
        {
            while (queue.Count > 0)
            {
                var (message, _) = queue.Dequeue();
                Context.System.DeadLetters.Tell(message);
            }

            _buffers.Remove(entityId);
        }
    }

    private void RecordRestart(string entityId)
    {
        if (!_restartTimestamps.TryGetValue(entityId, out var timestamps))
        {
            timestamps = [];
            _restartTimestamps[entityId] = timestamps;
        }

        timestamps.Add(_timeProvider.GetUtcNow());
    }

    private bool IsRestartLimitExceeded(string entityId)
    {
        if (!_restartTimestamps.TryGetValue(entityId, out var timestamps))
        {
            return false;
        }

        var cutoff = _timeProvider.GetUtcNow() - _options.RestartWindow;
        timestamps.RemoveAll(t => t < cutoff);
        return timestamps.Count >= _options.MaxRestarts;
    }

    private string? FindEntityId(IActorRef actorRef)
    {
        foreach (var (entityId, state) in _entities)
        {
            if (state.Ref.Equals(actorRef))
            {
                return entityId;
            }
        }

        return null;
    }

    private void RemoveEntity(IActorRef actorRef)
    {
        var entityId = FindEntityId(actorRef);
        if (entityId is not null)
        {
            _entities.Remove(entityId);
            EntityRegionMetrics.ActiveEntities.Add(-1, new KeyValuePair<string, object?>("region", _regionTag));
        }
    }

    private void StartDrain()
    {
        Timers.Cancel(PassivationTimerKey);
        Become(Draining);

        foreach (var (_, state) in _entities)
        {
            state.Ref.Tell(PoisonPill.Instance);
        }

        if (_entities.Count == 0)
        {
            Context.Stop(Self);
        }
    }
}
