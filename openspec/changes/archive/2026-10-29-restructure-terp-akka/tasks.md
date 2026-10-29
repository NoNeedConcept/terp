## 1. Create folder structure

- [x] 1.1 Create `src/Terp.Akka/Hosting/` directory
- [x] 1.2 Create `src/Terp.Akka/Entities/` directory

## 2. Move Hosting types

- [x] 2.1 Move `ActorSystemSetup.cs` to `Hosting/`, update namespace to `Terp.Akka.Hosting`
- [x] 2.2 Move `IActorRegistration.cs` to `Hosting/`, update namespace to `Terp.Akka.Hosting`
- [x] 2.3 Move `ActorRegistrationExtensions.cs` to `Hosting/`, update namespace to `Terp.Akka.Hosting`

## 3. Move Entity types

- [x] 3.1 Move `EntityRegionActor.cs` to `Entities/`, update namespace to `Terp.Akka.Entities`
- [x] 3.2 Move `EntityRegionOptions.cs` to `Entities/`, update namespace to `Terp.Akka.Entities`
- [x] 3.3 Move `EntityRegionMetrics.cs` to `Entities/`, update namespace to `Terp.Akka.Entities`
- [x] 3.4 Move `EntityIdExtractor.cs` to `Entities/`, update namespace to `Terp.Akka.Entities`
- [x] 3.5 Move `EntityRegionExtensions.cs` to `Entities/`, update namespace to `Terp.Akka.Entities`

## 4. Fix cross-namespace references

- [x] 4.1 Add `using Terp.Akka.Hosting;` / `using Terp.Akka.Entities;` where needed within `src/Terp.Akka/`
- [x] 4.2 Update `using` statements in `src/Terp.Akka.Tests/` for moved types
- [x] 4.3 Update `using` statements in `src/Terp.Akka.Example/` for moved types

## 5. API snapshot tests

- [x] 5.1 Run `dotnet test` for `Terp.API.Tests`, review `.received.` diff for correctness
- [x] 5.2 Rename `.received.txt` to `.verified.txt` to accept the new snapshot

## 6. Verify

- [x] 6.1 `dotnet build src/Terp.slnx` — zero warnings
- [x] 6.2 `dotnet test --solution Terp.slnx` (from `src/`) — all tests pass
- [x] 6.3 `dotnet format Terp.slnx --verify-no-changes` (from `src/`) — no format violations (pre-existing line-ending issues in Terp.Tests unrelated to this change)
- [x] 6.4 Commit: `refactor!: reorganize Terp.Akka into Hosting and Entities namespaces`
