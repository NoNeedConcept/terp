## Context

The VitePress docs site at `terp.noneedconcept.org` uses "Hosting" as the label for the `Terp` package section in the top-nav, hero button, and sidebar. The URL path is `/hosting/`. The `Terp` package contains hosting, diagnostics, resilience, threading, network, and date helpers — "Hosting" undersells it.

The site is pre-release with no indexed URLs, so renaming the path has zero SEO cost and needs no redirects.

## Goals / Non-Goals

**Goals:**
- Rename all user-visible "Hosting" labels to "Terp" in nav, sidebar, and hero
- Move `docs/hosting/` → `docs/terp/` so URL becomes `/terp/` (symmetric with `/akka/`)
- Update all internal `(/hosting/...)` links across all docs pages

**Non-Goals:**
- Changing the `Terp.Hosting` namespace in code (stays as-is, it's a C# namespace not a docs label)
- Adding redirects (site is pre-release, no existing traffic)
- Restructuring page content or adding new pages
- Touching code, tests, or API surface — this is docs-only

## Decisions

**Folder rename `hosting/` → `terp/`**: A simple `git mv` since VitePress derives URLs from the folder structure. All three files (`index.md`, `setups.md`, `gates.md`) move together. The internal content of these pages stays the same — they still describe `Terp.Hosting` namespace functionality, just live under the `/terp/` section.

**No redirects**: The site is not yet publicly indexed. Adding a VitePress redirect plugin would be a new dependency for zero benefit.

## Risks / Trade-offs

**Risk**: Stale links in external references (AGENTS.md, README, etc.) → Mitigation: grep the full repo for `/hosting/` paths and update them. The proposal already enumerates all affected files.

**Risk**: `docs/hosting/setups.md` still says "see [Hosting](/hosting/)" internally → Mitigation: the link update task covers all `.md` files under `docs/`.
