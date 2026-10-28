## 1. Interface and Extensions

- [x] 1.1 Create `src/Terp.Akka/IActorRegistration.cs` — interface with `Configure(AkkaConfigurationBuilder, IServiceProvider)`, XML docs
- [x] 1.2 Create `src/Terp.Akka/ActorRegistrationExtensions.cs` — static class with `AddActorRegistration<T>(this IServiceCollection)` and `ApplyRegistrations(this AkkaConfigurationBuilder, IServiceProvider)`, XML docs, null guards

## 2. Tests

- [x] 2.1 Add `ActorRegistrationExtensionsTests` in `src/Terp.Akka.Tests/` — covers: null args throw, single registration dispatched, multiple registrations dispatched in order, no registrations is no-op, AddActorRegistration adds correct descriptor
- [x] 2.2 Update API snapshot: run tests, review `.received.` diff, rename to `.verified.txt`

## 3. Example Update

- [x] 3.1 Refactor `Terp.Akka.Example` to demonstrate the pattern — extract a `GreeterActorRegistration : IActorRegistration` and use `AddActorRegistration<GreeterActorRegistration>()` + `ApplyRegistrations` in `ExampleActorSystem.BuildSystem`

## 4. Documentation

- [x] 4.1 Add `docs/akka/actor-registration.md` — usage guide with the library/host pattern
- [x] 4.2 Update `AGENTS.md` — document `IActorRegistration` pattern, adjust "no contributor setups" statement

## 5. Verify

- [x] 5.1 `dotnet build src/Terp.slnx -p:ContinuousIntegrationBuild=true` — zero warnings
- [x] 5.2 `dotnet test --solution Terp.slnx` (from `src/`) — all tests pass (184/184)
- [x] 5.3 `dotnet format src/Terp.slnx --verify-no-changes` — new files clean (pre-existing CRLF in EntityRegionExtensions.cs unrelated)
- [ ] 5.4 Commit with `feat(akka): add IActorRegistration for modular actor topology`
