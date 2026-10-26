# Terp brand

Terp is a small .NET library by NoNeedConcept for host setup, startup gates, metrics and Akka.NET. A terp is an artificial mound in the Frisian marshes, raised layer by layer above the flood line. The mark is its profile: flat land, raised in one place.

## Mark

The profile line, one shape, one colour. Built on a 64×64 grid.

- `logo/terp-mark.svg` – ochre, default
- `logo/terp-mark-ink.svg` – ink, for light grounds where ochre is too weak
- `logo/terp-mark-paper.svg` – paper, for photos or saturated grounds
- `logo/terp-lockup-light.png`, `logo/terp-lockup-dark.png` – mark + wordmark

Keep clear space of at least half the mark's height on all sides. Don't rotate, outline, add gradients or shadows, or recolour outside the palette.

## Wordmark

`terp`, always lowercase, Geist SemiBold (600), tracking −4.5 %. Mark sits left of the wordmark at cap height, gap ≈ 0.2 × mark width.

## Colour

| Name | Hex | Use |
| --- | --- | --- |
| Mud | `#C98B2B` | Mark, accent, highlights |
| Mud deep | `#8F5F14` | Links and accent text on paper |
| Ink | `#1A1813` | Text, dark grounds, icon background |
| Paper | `#F3EFE6` | Background, text on ink |

Use Mud for marks and large type only. For body-size accent text on Paper, use Mud deep.

## Type

- Geist – headings and body
- Geist Mono – code, package names, labels

Both are free on Google Fonts.

## Icons

- `icon/terp-icon.svg`, `icon/terp-icon-512.png` – NuGet icon for `Terp` (ochre on ink)
- `icon/terp-akka-icon.svg`, `icon/terp-akka-icon-512.png` – NuGet icon for `Terp.Akka` (inverted)
- `favicon/favicon.svg`, `favicon-16/32/48.png` – heavier cut of the line so it holds at 16 px
- `favicon/apple-touch-icon-180.png`

```html
<link rel="icon" href="/favicon.svg" type="image/svg+xml">
<link rel="icon" href="/favicon-32.png" sizes="32x32">
<link rel="apple-touch-icon" href="/apple-touch-icon-180.png">
```

In the `.csproj`:

```xml
<PackageIcon>terp-icon-512.png</PackageIcon>
<ItemGroup><None Include="terp-icon-512.png" Pack="true" PackagePath="\" /></ItemGroup>
```

## GitHub and README

- `social/terp-social-preview.png` – 1280×640, upload under Settings → Social preview
- `readme/readme-header-light.png`, `readme/readme-header-dark.png`

```html
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="readme-header-dark.png">
  <img alt="terp" src="readme-header-light.png">
</picture>
```

## VitePress

```css
:root {
  --vp-c-brand-1: #8F5F14;
  --vp-c-brand-2: #C98B2B;
  --vp-c-bg: #F3EFE6;
  --vp-font-family-base: 'Geist', sans-serif;
  --vp-font-family-mono: 'Geist Mono', monospace;
}
.dark {
  --vp-c-brand-1: #C98B2B;
  --vp-c-bg: #1A1813;
}
```

Use `logo/terp-mark.svg` as `themeConfig.logo` and as the hero image.

## Relation to NNC

Terp stands next to the NNC mark, never inside it.
