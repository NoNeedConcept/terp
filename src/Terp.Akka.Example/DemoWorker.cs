using Akka.Actor;
using Akka.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Terp.Akka.Example;

internal sealed class DemoWorker(ActorSystem system, IActorRegistry registry, IHostApplicationLifetime lifetime,
    ILogger<DemoWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var greeter = await registry.GetAsync<GreeterActor>(stoppingToken);   // waits until the actor system has started
        var reply = await greeter.Ask<Done>(new Greet("Terp"), TimeSpan.FromSeconds(5), stoppingToken);
        logger.LogInformation("Greeter says: {Reply}", reply.Result);

        // The backoff supervisor restarts the failing child with growing delays (100ms, 200ms, 400ms, ...).
        var result = new TaskCompletionSource<string>();
        var flaky = system.ActorOf(Props.Create(() => new FlakyActor(result))
            .BackoffOnFailure("flaky", TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), randomFactor: 0.1));

        // Keep poking until the child is back and finally succeeds (messages during a backoff go to dead letters).
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        while (!result.Task.IsCompleted && !timeout.IsCancellationRequested)
        {
            flaky.Tell(new Run());
            await Task.WhenAny(result.Task, Task.Delay(300, timeout.Token));
        }

        logger.LogInformation("Flaky actor {Result}", result.Task.IsCompleted ? result.Task.Result : "never succeeded");

        lifetime.StopApplication();
    }
}
