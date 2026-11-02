## Purpose

Defines the `SetupChain<TTarget>` fluent API for applying `ISetup` instances to host builders and applications while preserving concrete target types across the chain.

## Requirements

### Requirement: SetupChain preserves concrete target type

`SetupChain<TTarget>` is a `readonly struct` in namespace `Terp.Hosting` (`Terp` package). It wraps a target and preserves its generic type parameter across chained `Add` calls. The struct SHALL NOT be directly constructable by consumers (internal constructor only).

#### Scenario: Builder type preserved through chain
- **WHEN** `builder.Setups().Add<SetupA>().Add<SetupB>()` is called where `builder` is a concrete builder type (e.g. `WebApplicationBuilder`)
- **THEN** the chain's `TTarget` remains the concrete builder type throughout, and the builder retains its concrete type for subsequent calls like `Build()`

#### Scenario: Application type preserved through chain
- **WHEN** `app.Setups().Add<SetupA>()` is called where `app` is a concrete application type (e.g. `WebApplication`)
- **THEN** the chain's `TTarget` remains the concrete application type

### Requirement: Setups entry point for builders

An extension method `Setups<TBuilder>(this TBuilder builder)` where `TBuilder : IHostApplicationBuilder` SHALL return `SetupChain<TBuilder>` with `TBuilder` inferred from the receiver.

#### Scenario: Entry point infers type from receiver
- **WHEN** a `WebApplicationBuilder` calls `.Setups()`
- **THEN** the return type is `SetupChain<WebApplicationBuilder>`, not `SetupChain<IHostApplicationBuilder>`

#### Scenario: Null builder
- **WHEN** `Setups()` is called on a null builder reference
- **THEN** an `ArgumentNullException` SHALL be thrown

### Requirement: Setups entry point for applications

An extension method `Setups<TApplication>(this TApplication app)` where `TApplication : IHost` SHALL return `SetupChain<TApplication>` with `TApplication` inferred from the receiver.

#### Scenario: Entry point infers type from receiver
- **WHEN** an `IHost` implementation calls `.Setups()`
- **THEN** the return type is `SetupChain<TApplication>` with the concrete type

#### Scenario: Null application
- **WHEN** `Setups()` is called on a null application reference
- **THEN** an `ArgumentNullException` SHALL be thrown

### Requirement: Add with generic type parameter

`SetupChain<TTarget>.Add<TSetup>()` where `TSetup : ISetup, new()` SHALL create a new instance of `TSetup` and apply it immediately to the target. It SHALL return the same `SetupChain<TTarget>` for chaining.

#### Scenario: Single-generic Add on builder
- **WHEN** `builder.Setups().Add<MySetup>()` is called where `MySetup : ISetup, ISetup<IServiceCollection>`
- **THEN** `MySetup.Setup(builder.Services, context)` is invoked immediately

#### Scenario: Chained Adds
- **WHEN** `builder.Setups().Add<SetupA>().Add<SetupB>().Add<SetupC>()` is called
- **THEN** each setup is applied in call order (A, then B, then C)

### Requirement: Add with instance

`SetupChain<TTarget>.Add(ISetup setup)` SHALL apply the given setup instance immediately to the target. It SHALL return the same `SetupChain<TTarget>` for chaining. It SHALL throw `ArgumentNullException` if `setup` is null.

#### Scenario: Instance Add with constructor dependencies
- **WHEN** `builder.Setups().Add(new CustomSetup(connectionString))` is called
- **THEN** the setup instance is applied immediately with its injected dependencies

#### Scenario: Null setup instance
- **WHEN** `Add(null!)` is called
- **THEN** an `ArgumentNullException` SHALL be thrown

### Requirement: Builder dispatch targets and order

When `Add` is called on a builder chain, the setup SHALL be dispatched to the following targets in this fixed order, matching by `is ISetup<T>` pattern:

1. `IConfigurationManager` (from `builder.Configuration`)
2. `ILoggingBuilder` (from `builder.Logging`)
3. `IServiceCollection` (from `builder.Services`)
4. `IMetricsBuilder` (from `builder.Metrics`)
5. `TBuilder` (the builder itself)

A setup that matches none of these SHALL cause an `InvalidOperationException`. A setup MAY match multiple targets; all matching targets are applied in the order above.

#### Scenario: Multi-target setup applies in order
- **WHEN** a setup implements both `ISetup<IConfigurationManager>` and `ISetup<IServiceCollection>`
- **THEN** configuration is set up first, then services

#### Scenario: No matching target throws
- **WHEN** a setup implements only `ISetup` (the marker) with no `ISetup<T>` for any supported target
- **THEN** an `InvalidOperationException` SHALL be thrown naming the setup type and builder type

#### Scenario: Builder-typed setup
- **WHEN** a setup implements `ISetup<TBuilder>` where `TBuilder` matches the chain's target type
- **THEN** it receives the concrete builder instance and a `SetupContext`

### Requirement: Application dispatch target

When `Add` is called on an application chain, the setup SHALL be matched against `ISetup<TApplication>`. If it does not match, an `InvalidOperationException` SHALL be thrown.

#### Scenario: Matching application setup
- **WHEN** a setup implements `ISetup<TApplication>` matching the chain's target
- **THEN** `Setup(app, context)` is called with configuration and environment from `app.Services`

#### Scenario: Contravariant application setup
- **WHEN** a setup implements `ISetup<IHost>` and the chain target is a subtype of `IHost`
- **THEN** the setup matches via contravariance and is applied

#### Scenario: Non-matching application setup throws
- **WHEN** a setup does not implement `ISetup<TApplication>` for the chain's target type
- **THEN** an `InvalidOperationException` SHALL be thrown

### Requirement: SetupContext construction

For builder chains, `SetupContext` SHALL be constructed from `builder.Configuration` and `builder.Environment`. For application chains, `SetupContext` SHALL be constructed from `app.Services.GetRequiredService<IConfiguration>()` and `app.Services.GetRequiredService<IHostEnvironment>()`. This matches the existing behavior.

#### Scenario: Builder context
- **WHEN** a setup is applied via a builder chain
- **THEN** `SetupContext.Configuration` is `builder.Configuration` and `SetupContext.Environment` is `builder.Environment`

#### Scenario: Application context
- **WHEN** a setup is applied via an application chain
- **THEN** `SetupContext.Configuration` and `SetupContext.Environment` come from the application's DI container

### Requirement: AOT and trim safety

`SetupChain<TTarget>` and all dispatch logic SHALL remain reflection-free. The implementation SHALL use only `is` pattern matching for type dispatch. No `Type.GetInterfaces()`, `Activator.CreateInstance`, or other reflection APIs.

#### Scenario: Terp.Hosting.Example publishes with AOT
- **WHEN** `Terp.Hosting.Example` is published with `PublishAot` and migrated to the new API
- **THEN** it compiles with 0 trim/AOT warnings

### Requirement: Removal of Setup extension methods

All `Setup` extension methods in `SetupExtensions` and `ApplicationSetupExtensions` SHALL be removed (6 methods total: instance, single-generic, two-generic on both builder and app sides).

#### Scenario: Old API no longer compiles
- **WHEN** consumer code calls `builder.Setup(new X())` or `builder.Setup<X>()` or `app.Setup(new X())`
- **THEN** it fails to compile, directing the consumer to use `Setups().Add<X>()` or `Setups().Add(new X())`
