## 1. Rename folder

- [ ] 1.1 `git mv docs/hosting docs/terp` — move the three files (index.md, setups.md, gates.md)

## 2. Update VitePress config

- [ ] 2.1 In `docs/.vitepress/config.ts`: change nav item `{ text: 'Hosting', link: '/hosting/' }` → `{ text: 'Terp', link: '/terp/' }` and `activeMatch` from `^/hosting/` to `^/terp/`
- [ ] 2.2 In `docs/.vitepress/config.ts`: change sidebar group `text: 'Hosting'` → `text: 'Terp'` and all sidebar links from `/hosting/` to `/terp/`

## 3. Update landing page

- [ ] 3.1 In `docs/index.md`: change hero button text from `Hosting` to `Terp` and link from `/hosting/` to `/terp/`
- [ ] 3.2 In `docs/index.md`: update "Where to start" links from `/hosting/` to `/terp/`

## 4. Update internal links

- [ ] 4.1 In `docs/getting-started.md`: update all `/hosting/` links to `/terp/` (4 occurrences)
- [ ] 4.2 In `docs/terp/index.md`: update internal `/hosting/` links to `/terp/` (3 occurrences)
- [ ] 4.3 In `docs/terp/setups.md`: update `/hosting/` link to `/terp/`
- [ ] 4.4 In `docs/utilities/resilience.md`: update `/hosting/gates` link to `/terp/gates`

## 5. Verify

- [ ] 5.1 Grep the full `docs/` directory for any remaining `/hosting/` references (excluding node_modules and .vitepress/dist)
- [ ] 5.2 Run `pnpm docs:build` (or equivalent VitePress build) and confirm no broken links. Commit scope: `docs`
