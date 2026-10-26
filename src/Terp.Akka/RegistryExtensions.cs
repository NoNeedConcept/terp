using Akka.Actor;
using Akka.Hosting;

namespace Terp.Akka;

/// <summary>
/// Extensions for looking up actors in the Akka.Hosting <see cref="ActorRegistry"/> of an <see cref="ActorSystem"/>.
/// </summary>
public static class RegistryExtensions
{
    /// <summary>Gets the actor registered under <typeparamref name="T"/> in the actor registry.</summary>
    /// <typeparam name="T">The registry key type.</typeparam>
    /// <param name="context">The actor context whose system registry is used.</param>
    /// <exception cref="MissingActorRegistryEntryException">No actor is registered for <typeparamref name="T"/>.</exception>
    public static IActorRef GetActor<T>(this IActorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.System.GetActor<T>();
    }

    /// <summary>Gets the actor registered under <typeparamref name="T"/> in the actor registry.</summary>
    /// <typeparam name="T">The registry key type.</typeparam>
    /// <param name="system">The actor system whose registry is used.</param>
    /// <exception cref="MissingActorRegistryEntryException">No actor is registered for <typeparamref name="T"/>.</exception>
    public static IActorRef GetActor<T>(this ActorSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        return system.GetRegistry().Get<T>();
    }

    /// <summary>Tries to get the actor registered under <typeparamref name="T"/> in the actor registry.</summary>
    /// <typeparam name="T">The registry key type.</typeparam>
    /// <param name="context">The actor context whose system registry is used.</param>
    /// <param name="actor">The registered actor, or <see cref="ActorRefs.Nobody"/> if none is registered.</param>
    /// <returns><see langword="true"/> if an actor is registered for <typeparamref name="T"/>; otherwise <see langword="false"/>.</returns>
    public static bool TryGetActor<T>(this IActorContext context, out IActorRef actor)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.System.TryGetActor<T>(out actor);
    }

    /// <summary>Tries to get the actor registered under <typeparamref name="T"/> in the actor registry.</summary>
    /// <typeparam name="T">The registry key type.</typeparam>
    /// <param name="system">The actor system whose registry is used.</param>
    /// <param name="actor">The registered actor, or <see cref="ActorRefs.Nobody"/> if none is registered.</param>
    /// <returns><see langword="true"/> if an actor is registered for <typeparamref name="T"/>; otherwise <see langword="false"/>.</returns>
    public static bool TryGetActor<T>(this ActorSystem system, out IActorRef actor)
    {
        ArgumentNullException.ThrowIfNull(system);

        return system.GetRegistry().TryGet<T>(out actor);
    }

    /// <summary>Tries to get the actor registered under <paramref name="key"/> in the actor registry.</summary>
    /// <param name="context">The actor context whose system registry is used.</param>
    /// <param name="key">The registry key type.</param>
    /// <param name="actor">The registered actor, or <see cref="ActorRefs.Nobody"/> if none is registered.</param>
    /// <returns><see langword="true"/> if an actor is registered for <paramref name="key"/>; otherwise <see langword="false"/>.</returns>
    public static bool TryGetActor(this IActorContext context, Type key, out IActorRef actor)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        return context.System.TryGetActor(key, out actor);
    }

    /// <summary>Tries to get the actor registered under <paramref name="key"/> in the actor registry.</summary>
    /// <param name="system">The actor system whose registry is used.</param>
    /// <param name="key">The registry key type.</param>
    /// <param name="actor">The registered actor, or <see cref="ActorRefs.Nobody"/> if none is registered.</param>
    /// <returns><see langword="true"/> if an actor is registered for <paramref name="key"/>; otherwise <see langword="false"/>.</returns>
    public static bool TryGetActor(this ActorSystem system, Type key, out IActorRef actor)
    {
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(key);

        return system.GetRegistry().TryGet(key, out actor);
    }

    /// <summary>Gets the actor registered under <typeparamref name="T"/>, waiting until it is registered if necessary.</summary>
    /// <typeparam name="T">The registry key type.</typeparam>
    /// <param name="context">The actor context whose system registry is used.</param>
    /// <returns>A task that completes with the actor as soon as it is registered; it does not throw for missing entries.</returns>
    public static Task<IActorRef> GetActorAsync<T>(this IActorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.System.GetActorAsync<T>();
    }

    /// <summary>Gets the actor registered under <typeparamref name="T"/>, waiting until it is registered if necessary.</summary>
    /// <typeparam name="T">The registry key type.</typeparam>
    /// <param name="system">The actor system whose registry is used.</param>
    /// <returns>A task that completes with the actor as soon as it is registered; it does not throw for missing entries.</returns>
    public static Task<IActorRef> GetActorAsync<T>(this ActorSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        return system.GetRegistry().GetAsync<T>();
    }

    /// <summary>Gets the read-only actor registry of the actor system.</summary>
    /// <param name="system">The actor system.</param>
    /// <returns>The <see cref="ActorRegistry"/> extension of <paramref name="system"/>, exposed read-only.</returns>
    public static IReadOnlyActorRegistry GetRegistry(this ActorSystem system)
    {
        ArgumentNullException.ThrowIfNull(system);

        return ActorRegistry.For(system);
    }
}
