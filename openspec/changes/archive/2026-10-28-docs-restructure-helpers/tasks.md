## 1. Create utilities directory and move files

- [x] 1.1 Create `docs/utilities/` directory
- [x] 1.2 Move `docs/resilience/index.md` → `docs/utilities/resilience.md` (update frontmatter title if needed)
- [x] 1.3 Merge `docs/network/index.md`, `docs/datetime/index.md`, `docs/threading/index.md` into `docs/utilities/helpers.md` with `##` headers per feature
- [x] 1.4 Remove old directories: `docs/network/`, `docs/datetime/`, `docs/threading/`, `docs/resilience/`

## 2. Update Diagnostics page

- [x] 2.1 Add CounterExtensions section to `docs/diagnostics/index.md` — `.Up()` / `.Down()`, tag overloads, type-safety note

## 3. Update sidebar config

- [x] 3.1 Update `docs/.vitepress/config.ts` — replace "Terp helpers" group with "Observability" and "Utilities" groups

## 4. Verify

- [x] 4.1 Run `npm run docs:build` (or VitePress build) — zero errors, all pages resolve
- [x] 4.2 Commit with `docs: restructure helpers into Observability and Utilities groups`
