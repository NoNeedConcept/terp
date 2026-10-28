# docs-counter-extensions Specification

## Purpose
TBD - created by archiving change docs-restructure-helpers. Update Purpose after archive.
## Requirements
### Requirement: Document CounterExtensions on Diagnostics page

The Diagnostics page (`docs/diagnostics/index.md`) SHALL include a section documenting the `CounterExtensions` class from `Terp.Diagnostics`.

#### Scenario: CounterExtensions section exists
- **WHEN** reading the Diagnostics page
- **THEN** it SHALL contain a section covering `.Up()` on `Counter<T>`, `.Up()` and `.Down()` on `UpDownCounter<T>`

#### Scenario: Tag overloads shown
- **WHEN** reading the CounterExtensions section
- **THEN** it SHALL show examples with 0, 1, and 2 tag overloads

#### Scenario: Type safety documented
- **WHEN** reading the CounterExtensions section
- **THEN** it SHALL explain that `.Down()` is only available on `UpDownCounter<T>`, not on `Counter<T>`

