using Akka.Actor;
using Akka.Event;

namespace Terp.Akka.Example;

/// <summary>Fails on its first starts, then works. The static counter survives restarts.</summary>
internal sealed class FlakyActor : ReceiveActor
{
    private static int _starts;

    public FlakyActor(TaskCompletionSource<string> result)
    {
        var log = Context.GetLogger();
        var attempt = Interlocked.Increment(ref _starts);

        Receive<Run>(_ =>
        {
            if (attempt < 4)
            {
                log.Warning("Flaky attempt {0} failed, the backoff supervisor restarts it", attempt);
                throw new InvalidOperationException($"attempt {attempt}");
            }

            log.Info("Flaky attempt {0} succeeded", attempt);
            result.TrySetResult($"succeeded on attempt {attempt}");
        });
    }
}
