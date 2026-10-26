using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Terp.Akka.Example;
using Terp.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders().AddSimpleConsole(o => o.SingleLine = true);
builder.Services.AddSingleton<IGreeter, Greeter>();     // dependency injected into the actor
builder.Services.AddHostedService<DemoWorker>();

builder.Setup(new ExampleActorSystem());         // ActorSystemSetup subclass owns the one actor system

using var host = builder.Build();

host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping
    .Register(() => Console.WriteLine("Stopping: the actor system shuts down with the host"));

await host.RunAsync();
