using Microsoft.Extensions.Hosting;
using Terp.Hosting;
using Terp.Hosting.Example;

var builder = Host.CreateApplicationBuilder(args);

builder.Setup<DemoSetup>();
builder.Setup<WorkerSetup>();

using var host = builder.Build();
await host.RunAsync();
