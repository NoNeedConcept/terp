## ADDED Requirements

### Requirement: Merge small helpers into single page

The content from `docs/network/index.md`, `docs/datetime/index.md` and `docs/threading/index.md` SHALL be merged into a single page at `docs/utilities/helpers.md`.

#### Scenario: Helpers page contains all three features
- **WHEN** reading `/utilities/helpers`
- **THEN** it SHALL contain sections for PortFinder (Network), DateTimeExtensions and AsyncLock (Threading)

#### Scenario: Original directories removed
- **WHEN** checking the docs file structure
- **THEN** the directories `docs/network/`, `docs/datetime/` and `docs/threading/` SHALL NOT exist

### Requirement: Move Resilience page

The content from `docs/resilience/index.md` SHALL move to `docs/utilities/resilience.md`.

#### Scenario: Resilience page at new path
- **WHEN** navigating to `/utilities/resilience`
- **THEN** the Backoff and BackoffPolicy documentation SHALL be displayed

#### Scenario: Original directory removed
- **WHEN** checking the docs file structure
- **THEN** the directory `docs/resilience/` SHALL NOT exist
