# actor-registration Specification

## Purpose
TBD - created by archiving change actor-registration. Update Purpose after archive.
## Requirements
### Requirement: IActorRegistration interface
The `Terp.Akka` package SHALL provide a public `IActorRegistration` interface in the `Terp.Akka` namespace with a single method:

```csharp
public interface IActorRegistration
{
    void Configure(AkkaConfigurationBuilder builder, IServiceProvider provider);
}
```

The interface SHALL NOT extend `ISetup` or any other Terp interface. Implementors MUST be `sealed` classes per project conventions.

#### Scenario: Library implements IActorRegistration
- **WHEN** a library creates a class implementing `IActorRegistration`
- **THEN** the class can configure actors on the `AkkaConfigurationBuilder` in its `Configure` method using `WithActors`, `WithEntityRegion<T>`, or any other Akka.Hosting builder API

#### Scenario: Null arguments throw
- **WHEN** `Configure` is called by `ApplyRegistrations` with a non-null builder and provider
- **THEN** the implementation receives valid, non-null arguments
- **WHEN** a consumer implements `Configure` and receives null arguments
- **THEN** the implementation SHOULD throw `ArgumentNullException` per project conventions (`ArgumentNullException.ThrowIfNull`)

### Requirement: AddActorRegistration convenience extension
The `Terp.Akka` package SHALL provide an extension method on `IServiceCollection`:

```csharp
public static IServiceCollection AddActorRegistration<T>(this IServiceCollection services)
    where T : class, IActorRegistration, new()
```

The method SHALL register `T` as a singleton implementation of `IActorRegistration` via `AddSingleton<IActorRegistration, T>()`. The method SHALL return the `IServiceCollection` for chaining. The method SHALL throw `ArgumentNullException` if `services` is null.

Libraries that need constructor-injected registrations SHALL use `AddSingleton<IActorRegistration, T>()` directly — this convenience method covers the parameterless-constructor case.

#### Scenario: Register a parameterless actor registration
- **WHEN** a library calls `services.AddActorRegistration<MyActorRegistration>()` where `MyActorRegistration` is a sealed class with a parameterless constructor implementing `IActorRegistration`
- **THEN** the service collection contains a singleton `IActorRegistration` descriptor for `MyActorRegistration`

#### Scenario: Multiple registrations from different libraries
- **WHEN** library A calls `services.AddActorRegistration<AActors>()` and library B calls `services.AddActorRegistration<BActors>()`
- **THEN** the service collection contains two `IActorRegistration` descriptors, resolvable via `GetServices<IActorRegistration>()`

#### Scenario: Null services throws
- **WHEN** `AddActorRegistration<T>` is called on a null `IServiceCollection`
- **THEN** an `ArgumentNullException` is thrown with parameter name `services`

### Requirement: ApplyRegistrations builder extension
The `Terp.Akka` package SHALL provide an extension method on `AkkaConfigurationBuilder`:

```csharp
public static AkkaConfigurationBuilder ApplyRegistrations(
    this AkkaConfigurationBuilder builder,
    IServiceProvider provider)
```

The method SHALL resolve all `IActorRegistration` services from the provider via `GetServices<IActorRegistration>()` and call `Configure(builder, provider)` on each, in DI registration order. The method SHALL return the builder for fluent chaining. The method SHALL throw `ArgumentNullException` if `builder` or `provider` is null.

If no `IActorRegistration` services are registered, the method SHALL return the builder without error (no-op).

#### Scenario: Dispatches to all registered IActorRegistration instances
- **WHEN** a host calls `builder.ApplyRegistrations(provider)` and two `IActorRegistration` implementations are registered
- **THEN** `Configure` is called on both implementations with the same builder and provider

#### Scenario: Respects DI registration order
- **WHEN** registration A is added before registration B via `AddActorRegistration`
- **THEN** `ApplyRegistrations` calls A's `Configure` before B's `Configure`

#### Scenario: No registrations is a no-op
- **WHEN** `ApplyRegistrations` is called and no `IActorRegistration` services are registered
- **THEN** the method returns the builder without error

#### Scenario: Null builder throws
- **WHEN** `ApplyRegistrations` is called with a null builder
- **THEN** an `ArgumentNullException` is thrown with parameter name `builder`

#### Scenario: Null provider throws
- **WHEN** `ApplyRegistrations` is called with a null provider
- **THEN** an `ArgumentNullException` is thrown with parameter name `provider`

#### Scenario: Used explicitly in BuildSystem
- **WHEN** a host overrides `ActorSystemSetup.BuildSystem`
- **THEN** the host calls `builder.ApplyRegistrations(provider)` at the desired point in the builder chain — after infrastructure setup (logging, persistence, clustering) and before or after any inline actor wiring

### Requirement: No reflection and trim/AOT safe
The implementation SHALL NOT use reflection. All DI registrations use generic `AddSingleton<TService, TImplementation>()`. Service resolution uses `GetServices<T>()`. Interface dispatch is direct. This ensures compatibility with Native AOT and trimming, consistent with the existing `ActorSystemSetup` pattern.

#### Scenario: AOT compilation succeeds
- **WHEN** a project using `IActorRegistration`, `AddActorRegistration<T>`, and `ApplyRegistrations` is published with `PublishAot`
- **THEN** the publish completes with zero trim/AOT warnings from `Terp.Akka`

