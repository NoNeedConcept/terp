using Microsoft.Extensions.Hosting;
using Terp.Hosting;
using Terp.Hosting.Example;

var builder = Host.CreateApplicationBuilder(args);

builder
    .Setup(new DemoSetup())       // configuration, logging, services, metrics and builder, in that fixed order
    .Setup(new WorkerSetup());    // services: the startup gate and the worker

using var host = builder.Build();
await host.RunAsync();
