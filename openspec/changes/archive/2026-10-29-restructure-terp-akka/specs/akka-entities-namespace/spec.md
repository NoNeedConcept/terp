## ADDED Requirements

### Requirement: Entity region types live in Terp.Akka.Entities namespace

`EntityRegionActor`, `EntityRegionOptions`, `EntityRegionMetrics` (internal), `EntityIdExtractor`, and `EntityRegionExtensions` SHALL reside in namespace `Terp.Akka.Entities`, in the `Entities/` subfolder of the `Terp.Akka` project.

#### Scenario: Consumer configures an entity region

- **WHEN** a consumer registers an entity region using `WithEntityRegion<TKey>`
- **THEN** they MUST add `using Terp.Akka.Entities;` to access `EntityRegionExtensions`, `EntityRegionActor`, `EntityRegionOptions`, and `EntityIdExtractor`

#### Scenario: Consumer defines entity messages without Entities namespace

- **WHEN** a consumer defines a message record implementing `IWithEntityId`
- **THEN** only `using Terp.Akka;` is required — `IWithEntityId` is NOT in the `Entities` namespace

#### Scenario: Namespace matches folder

- **WHEN** the project is built with `EnforceCodeStyleInBuild` enabled
- **THEN** no IDE0130 warnings are emitted for files in `Entities/`

### Requirement: API snapshot reflects new namespaces

The API snapshot tests in `Terp.API.Tests` SHALL produce `.verified.txt` files that show all moved types under their new fully-qualified namespaces.

#### Scenario: Snapshot test passes after update

- **WHEN** `dotnet test` runs `Terp.API.Tests`
- **THEN** the Verify snapshots match the new namespace layout with no `.received.` files generated
