using Microsoft.Extensions.Hosting;
using Terp.Example;
using Terp.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Setups()
    .Add<DemoSetup>()
    .Add<WorkerSetup>();

using var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
