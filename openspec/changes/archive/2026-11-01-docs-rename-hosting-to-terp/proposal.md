## Why

The docs top-nav and hero button say "Hosting" for the `Terp` package section, but that package covers much more than hosting: diagnostics, resilience, threading, network helpers, and date extensions. The label misleads users into thinking the section only covers `ISetup<T>` and startup gates. Renaming it to "Terp" makes it symmetric with "Terp.Akka" and accurately represents the full package scope.

Commit type: `docs`

## What Changes

- Rename the top-nav item from "Hosting" to "Terp"
- Rename the hero button on the landing page from "Hosting" to "Terp"
- Move the docs folder from `docs/hosting/` to `docs/terp/` so the URL becomes `/terp/` (symmetric with `/akka/`)
- Update all internal links pointing to `/hosting/` → `/terp/`
- Update the sidebar group label from "Hosting" to "Terp"
- Update the `activeMatch` pattern in config from `^/hosting/` to `^/terp/`

This is a docs-only change. No code, no public API, no package changes.

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

_None — docs-only change, no spec-level behavior changes._

## Impact

- `docs/.vitepress/config.ts` — nav, sidebar, activeMatch
- `docs/index.md` — hero button text and link
- `docs/hosting/` → `docs/terp/` — folder rename (3 files: index.md, setups.md, gates.md)
- `docs/getting-started.md` — internal links to `/hosting/`
- Any other docs pages linking to `/hosting/` paths
- VitePress llms.txt plugin output changes URLs
- No redirects needed (site is pre-release, no indexed URLs yet)
