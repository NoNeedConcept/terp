## ADDED Requirements

### Requirement: Hosting types live in Terp.Akka.Hosting namespace

`ActorSystemSetup`, `IActorRegistration`, and `ActorRegistrationExtensions` SHALL reside in namespace `Terp.Akka.Hosting`, in the `Hosting/` subfolder of the `Terp.Akka` project.

#### Scenario: Consumer sets up an actor system

- **WHEN** a consumer configures an actor system using `ActorSystemSetup`
- **THEN** they MUST add `using Terp.Akka.Hosting;` to access `ActorSystemSetup`, `IActorRegistration`, and `ActorRegistrationExtensions`

#### Scenario: Namespace matches folder

- **WHEN** the project is built with `EnforceCodeStyleInBuild` enabled
- **THEN** no IDE0130 warnings are emitted for files in `Hosting/`

### Requirement: Root namespace retains general-purpose extensions

`ContextExtensions`, `RegistryExtensions`, `ResolveExtensions`, `BackoffExtensions`, `AkkaOptionsExtensions`, and `IWithEntityId` SHALL remain in namespace `Terp.Akka` in the project root.

#### Scenario: Extensions available without extra using

- **WHEN** a consumer has `using Terp.Akka;`
- **THEN** all general-purpose extension methods (`GetActor<T>`, `GetChild`, `ChildTell`, `BackoffOnFailure`, etc.) and `IWithEntityId` are accessible without additional `using` statements
