## Context

The docs site uses VitePress with file-based routing and `cleanUrls: true`. The sidebar is defined in `docs/.vitepress/config.ts` as a flat array of groups. Each doc page lives at `docs/<feature>/index.md`. The nav bar has: Getting started | Hosting | Terp.Akka | API.

Current sidebar groups: Start, Hosting, Terp helpers (5 items), Terp.Akka, API Reference, About.

## Goals / Non-Goals

**Goals:**

- Promote Diagnostics/Observability to first-class sidebar group
- Reduce sidebar noise by merging trivial helpers
- Add CounterExtensions to the Diagnostics page
- Keep existing URLs working (no broken links)

**Non-Goals:**

- No new recipe/pattern pages (separate change)
- No content rewrite of existing pages — only structural moves and CounterExtensions addition
- No nav bar changes (Observability isn't prominent enough yet for top nav)
- No Akka section changes

## Decisions

### File structure

Diagnostics stays at `docs/diagnostics/index.md` — same URL, just relabeled "Observability" in the sidebar. No file move needed.

Resilience moves to `docs/utilities/resilience.md`. The old `docs/resilience/index.md` content moves; the old directory can be removed.

Network, DateTime, Threading merge into `docs/utilities/helpers.md`. Content is concatenated with `##` headers per feature. The old directories (`docs/network/`, `docs/datetime/`, `docs/threading/`) are removed.

New directory: `docs/utilities/` with two files.

**Alternative considered:** Keep Resilience at `docs/resilience/`. Rejected — having it outside the Utilities group while being in the Utilities sidebar is confusing for contributors.

### Sidebar config

```ts
{
  text: 'Observability',
  items: [
    { text: 'Overview', link: '/diagnostics/' }
  ]
},
{
  text: 'Utilities',
  items: [
    { text: 'Resilience', link: '/utilities/resilience' },
    { text: 'Helpers', link: '/utilities/helpers' }
  ]
}
```

Single-item "Observability" group is intentional — it makes room for future pages (OTel wiring, HTTP metrics deep-dive) without another restructure.

### CounterExtensions documentation

Added as a new `## Counter extensions` section on the Diagnostics page, after the existing TimerScope/TimeAsync content. Shows `.Up()` / `.Down()` with tag overloads, the type-system enforcement (no `.Down()` on `Counter<T>`), and a before/after comparison.

## Risks / Trade-offs

- **[Broken links]** Old URLs `/resilience/`, `/network/`, `/datetime/`, `/threading/` will 404. → Mitigated: these are internal docs for a pre-1.0 library with minimal external links. The llms.txt plugin regenerates automatically.
- **[Single-item Observability group]** Looks sparse with one entry. → Acceptable as a placeholder; the future Observability content change will fill it.
- **[Merged helpers page length]** Network (30 lines) + DateTime (64 lines) + Threading (51 lines) = ~145 lines. → Comfortable single-page length, well within normal doc page size.
