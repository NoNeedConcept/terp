using Microsoft.Extensions.Hosting;
using Terp.Hosting;
using Terp.Hosting.Example;

var builder = Host.CreateApplicationBuilder(args);

builder.Setups()
    .Add<DemoSetup>()
    .Add<WorkerSetup>();

using var host = builder.Build();
await host.RunAsync().ConfigureAwait(false);
