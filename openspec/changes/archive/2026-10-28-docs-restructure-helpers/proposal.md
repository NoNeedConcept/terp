## Why

The "Terp helpers" sidebar group is a flat bag of 5 unrelated pages (Diagnostics, Resilience, Network, DateTime, Threading) with no hierarchy. Network (1 method, 30 lines), DateTime (6 methods) and Threading (1 class) each get a top-level sidebar entry equal in weight to the Hosting section. This inflates the navigation and buries Diagnostics — Terp's most substantial non-Hosting feature — between trivial utilities. The Diagnostics page also doesn't document the new `CounterExtensions` added in `6a8b5a9`.

**Commit type:** `docs` (no code changes, docs site only)

## What Changes

- **Remove the "Terp helpers" sidebar group** — split into two focused groups
- **Promote Diagnostics to "Observability"** — own top-level sidebar group, update page to include `CounterExtensions` (.Up/.Down)
- **Consolidate small helpers into "Utilities"** — Resilience keeps its own page (82 lines, substantial); Network, DateTime and Threading merge into a single "Helpers" page
- **Update VitePress sidebar config** (`docs/.vitepress/config.ts`) to reflect the new structure
- **Redirect-free** — VitePress `cleanUrls` means old `/diagnostics/` still resolves if the file stays at `docs/diagnostics/index.md`

### Sidebar: before → after

```
BEFORE                          AFTER
Terp helpers                    Observability
├── Diagnostics                 └── Overview (was Diagnostics, + CounterExtensions)
├── Resilience
├── Network                     Utilities
├── DateTime                    ├── Resilience (unchanged)
└── Threading                   └── Helpers (Network + DateTime + Threading merged)
```

## Capabilities

### New Capabilities

- `docs-sidebar-restructure`: Restructure the VitePress sidebar to replace "Terp helpers" with "Observability" and "Utilities" groups
- `docs-counter-extensions`: Document CounterExtensions (.Up/.Down) on the Diagnostics/Observability page
- `docs-helpers-consolidation`: Merge Network, DateTime and Threading into a single Helpers page

### Modified Capabilities

_(none — no spec-level behavior changes)_

## Impact

- **Docs only** — no library code, no public API, no tests
- **Files changed**: `docs/.vitepress/config.ts`, `docs/diagnostics/index.md`, new `docs/utilities/index.md` (merged helpers), removal of `docs/network/`, `docs/datetime/`, `docs/threading/` individual pages (or redirect stubs)
- **URL impact**: `/resilience/` moves to `/utilities/resilience`. `/network/`, `/datetime/`, `/threading/` merge into `/utilities/`. `/diagnostics/` stays at same path. Old URLs without trailing content still land somewhere due to VitePress file-based routing.
