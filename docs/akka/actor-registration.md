---
title: Actor registration
description: IActorRegistration lets libraries declare their actor topology without the host knowing the actor types.
---

# Actor registration

`IActorRegistration` lets a library declare its actors so the host can apply them without knowing the types directly. The library registers via DI, the host dispatches in `BuildSystem`.

```csharp
using Akka.Hosting;
using Terp.Akka.Hosting;
```

## Write a registration

A library implements `IActorRegistration` and registers actors on the builder:

```csharp
internal sealed class BudgetActorRegistration : IActorRegistration
{
    public void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
        builder.WithActors((system, registry, resolver) =>
            registry.Register<IBudgetTracker>(
                system.ActorOf(resolver.Props<BudgetTrackerActor>(), "budget-tracker")));
    }
}
```

Register it during service setup:

```csharp
services.AddActorRegistration<BudgetActorRegistration>();
```

This calls `AddSingleton<IActorRegistration, T>()` under the hood. For registrations that need constructor injection, use the DI overload directly:

```csharp
services.AddSingleton<IActorRegistration, BudgetActorRegistration>();
```

## Apply registrations in the host

The host calls `ApplyRegistrations` at the desired point in `BuildSystem`:

```csharp
internal sealed class AppActorSystem : ActorSystemSetup
{
    protected override string ActorSystemName => "my-app";

    protected override void BuildSystem(
        AkkaConfigurationBuilder builder, IServiceProvider serviceProvider)
    {
        builder
            .ConfigureLoggers(l => { l.ClearLoggers(); l.AddLoggerFactory(); })
            .WithJournalAndSnapshot(...)       // infrastructure first
            .ApplyRegistrations(serviceProvider); // then library actors
    }
}
```

`ApplyRegistrations` resolves all `IActorRegistration` services and calls `Configure` on each, in DI registration order. If no registrations are present, it is a no-op.

You can mix inline actor wiring and `ApplyRegistrations` freely. The host controls when registrations run relative to infrastructure setup.

## The pattern

```
┌─────────────────────────────────────────────────┐
│ Library                                          │
│                                                  │
│  services.AddActorRegistration<MyActors>()       │
│                                                  │
│  MyActors : IActorRegistration                   │
│    └─ Configure(builder, provider)               │
│         └─ builder.WithActors(...)               │
└──────────────────────┬──────────────────────────┘
                       │ DI
                       ▼
┌─────────────────────────────────────────────────┐
│ Host (ActorSystemSetup)                          │
│                                                  │
│  BuildSystem(builder, provider)                  │
│    └─ builder.ApplyRegistrations(provider)       │
│              │                                   │
│              ├─ LibA.Configure(builder, provider) │
│              └─ LibB.Configure(builder, provider) │
└─────────────────────────────────────────────────┘
```

## Related

- [Actor system](/akka/actor-system) for the `ActorSystemSetup` base class.
- [Registry and context](/akka/registry) for finding registered actors.
- [Entity regions](/akka/entity-region) for child-per-entity routing.
