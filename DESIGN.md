---
name: Pix
description: Real-time photobooth gallery — a dark, neon-accented canvas where the photo is the only thing that glows.
colors:
  flash-magenta: "#ec4899"
  flash-magenta-deep: "#db2777"
  void-black: "#18181b"
  paper-white: "#ffffff"
  frosted-panel: "rgba(255, 255, 255, 0.10)"
  recessed-well: "rgba(0, 0, 0, 0.20)"
  text-primary: "rgba(255, 255, 255, 0.90)"
  text-secondary: "rgba(255, 255, 255, 0.70)"
  text-muted: "rgba(255, 255, 255, 0.50)"
  text-faint: "rgba(255, 255, 255, 0.30)"
  neon-emerald: "#34d399"
  warning-coral: "#f87171"
  danger-strong: "#dc2626"
typography:
  display:
    fontFamily: "Poppins, ui-sans-serif, system-ui, sans-serif"
    fontSize: "1.5rem"
    fontWeight: 700
    lineHeight: 1.2
  headline:
    fontFamily: "Poppins, ui-sans-serif, system-ui, sans-serif"
    fontSize: "1.125rem"
    fontWeight: 600
    lineHeight: 1.3
  body:
    fontFamily: "Poppins, ui-sans-serif, system-ui, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Poppins, ui-sans-serif, system-ui, sans-serif"
    fontSize: "0.75rem"
    fontWeight: 600
    lineHeight: 1.4
  mono:
    fontFamily: "ui-monospace, SFMono-Regular, monospace"
    fontSize: "0.75rem"
    fontWeight: 400
    lineHeight: 1.4
rounded:
  pill: "9999px"
  md: "0.5rem"
  photo: "0.75rem"
spacing:
  xs: "0.5rem"
  sm: "0.75rem"
  md: "1rem"
  lg: "1.5rem"
  xl: "2rem"
components:
  button-primary:
    backgroundColor: "{colors.flash-magenta-deep}"
    textColor: "{colors.paper-white}"
    typography: "{typography.headline}"
    rounded: "{rounded.pill}"
    padding: "12px 24px"
  button-secondary:
    backgroundColor: "{colors.frosted-panel}"
    textColor: "{colors.text-secondary}"
    typography: "{typography.headline}"
    rounded: "{rounded.pill}"
    padding: "8px 16px"
  button-download:
    backgroundColor: "{colors.paper-white}"
    textColor: "{colors.void-black}"
    typography: "{typography.headline}"
    rounded: "{rounded.pill}"
    padding: "12px 24px"
  card:
    backgroundColor: "{colors.frosted-panel}"
    textColor: "{colors.text-primary}"
    rounded: "{rounded.md}"
    padding: "16px"
  input:
    backgroundColor: "{colors.frosted-panel}"
    textColor: "{colors.paper-white}"
    rounded: "{rounded.md}"
    padding: "8px 12px"
  badge-active:
    backgroundColor: "{colors.flash-magenta-deep}"
    textColor: "{colors.paper-white}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
  badge-inactive:
    backgroundColor: "{colors.frosted-panel}"
    textColor: "{colors.text-muted}"
    typography: "{typography.label}"
    rounded: "{rounded.pill}"
    padding: "4px 12px"
---

# Design System: Pix

## Overview

**Creative North Star: "The Neon Instant"**

Pix lives in the moment right after the flash — a dark room lit by one bright thing at a time. The
canvas is void-black (#18181b), the same tone as the venue itself: no card is whiter than it needs
to be, no section fights the photo for attention. Against that dark field, exactly one accent gets
to be loud — magenta (flash magenta on the canvas itself, its deeper shade under white text on a
filled surface — see Colors) — used the way a strobe is used: rare, sudden, and always on
something that matters (a primary action, an active state, a confirmed status). Everything else is
frosted glass: translucent white-on-black panels (`bg-white/10`) that read as UI chrome without
ever competing with what's printed on the "paper" above them.

The system is unapologetically tactile — full pill buttons, saturated fills, visible press feedback
(`active:scale-95`) — built for a phone tapped once in a dark room, not a mouse hovering a desktop.
Depth is rationed on purpose: everywhere that isn't a photograph is flat, tonal layering only; the
one place shadows and blur are allowed to exist is around an actual photo, which is the system's
only "object" — everything else is surface. Iconography leans on real emoji instead of a drawn icon
set, which keeps the voice playful and instantly legible in Spanish-first copy without adding an
icon library.

This is one visual language, not two: the guest-facing kiosk/wall/photo pages and the operator's
Spanish-language admin screens (agenda, finance, inventory) share the exact same tokens, components,
and rules. The admin side is not a "serious business" reskin — it's the same neon-instant room, just
with tables and forms instead of a QR code.

**Key Characteristics:**
- Void-black canvas, one magenta accent used sparingly and only for what matters most
- Frosted-glass panels (`white/10`) — flat, tonal, no shadows on UI chrome
- Full-pill buttons and badges; `rounded-lg` for containers; a dedicated, more generous radius
  reserved only for photographic imagery
- Depth (shadow, blur) exists only around real photos and the full-screen lightbox — a physical
  object in a flat room
- Tactile press feedback (`active:scale-95/98`) on every tappable element
- Emoji as the icon system, throughout guest and admin surfaces alike

## Colors

A near-monochrome dark stage with a single accent that is rationed, not decorative.

### Primary
Two shades of the same magenta split the work by whether text sits on top of a filled surface or
the color sits on the dark canvas directly — a WCAG AA audit (confirmed live, not theoretical: a
detector measured 3.5:1) found white text on the brighter shade fails accessible-contrast at
button/badge size, so the two shades are not interchangeable.
- **Flash Magenta Deep** (`#db2777`, ~4.6:1 with white text): the fill for every solid button and
  active/confirmed status badge — anywhere white text sits directly on the accent color.
- **Flash Magenta** (`#ec4899`, brighter): reserved for places the color sits on the void-black
  canvas itself rather than under white text — focus rings on inputs, small in-context text links
  ("register a deposit," "already counted as income"), and thin decorative accents (a progress-bar
  fill with no text on top of it). At that pairing (color-on-black), it already clears AA on its own
  and reads brighter/more "flash"-like, which suits a link or a ring better than a large fill would.
- Never use Flash Magenta as a large filled surface with white/light text on it — that is exactly
  the pairing the audit found failing.

### Neutral
- **Void Black** (`#18181b`): the canonical page background across every surface — also the PWA's
  `theme_color`/`background_color`, so the browser chrome itself matches the room.
- **Paper White** (`#ffffff`): reserved for the rare "printed paper" surface — the QR code's white
  card and the "Download" button, which is deliberately the one button styled like a physical print
  instead of the neon UI around it.
- **Frosted Panel** (`rgba(255,255,255,0.10)`): the default surface for every card, section, input,
  and secondary button. This is the system's only "elevated" surface, and it's not elevated at all —
  it's a tonal shift, not a shadow.
- **Recessed Well** (`rgba(0,0,0,0.20)`): a darker nested sub-panel inside a card (e.g. the deposits
  ledger inside an agenda entry) — signals "a panel within a panel" without introducing a border.
- **Text** at four opacities on white, used as the entire type-color scale instead of separate grays:
  primary text `white/90`, secondary `white/70`, muted metadata `white/50`, and faintest disclaimers
  or footers `white/30`.

### Semantic
- **Neon Emerald** (`#34d399`): income, positive profit, confirmed/success states in finance.
- **Warning Coral** (`#f87171`): expenses, negative profit, form-level danger text.
- **Danger Strong** (`#dc2626`, at 90% opacity as a fill): full-bleed error banners (login failure,
  list-load failure) — the one place a semantic color gets a solid background instead of just text.

### Named Rules
**The One Strobe Rule.** Flash Magenta lights up exactly one thing at a time — the primary action or
the confirmed state — never a background, a large panel, or more than one competing element in the
same view. Its rarity is what makes it read as "on."

**The Paper Exception Rule.** Paper White is reserved for things meant to feel like a physical print
leaving the system — the QR code and the "Descargar" button. Nothing else gets a white surface.

**The Deep-Fill Rule.** White text sits only on Flash Magenta Deep, never on the brighter Flash
Magenta — the bright shade drops below WCAG AA (3.5:1) under white text at button/badge size. The
bright shade is for color-on-black uses only (rings, links, thin decorative fills).

## Typography

**Display/Body Font:** Poppins (with `ui-sans-serif, system-ui, sans-serif` fallback)

**Character:** One geometric sans carries the entire system, from a kiosk hero heading down to a
form label — weight and size do the differentiating work instead of a second typeface. It reads as
confident and slightly playful (Poppins' rounded terminals), never clinical.

### Hierarchy
- **Display** (700, 1.5rem–1.875rem `text-2xl`/`text-3xl`, tight line-height): page titles ("Finanzas",
  "Agenda") and the kiosk's event-name hero.
- **Headline** (600, 1.125rem `text-lg`): card/section titles, button labels.
- **Body** (400, 1rem): default paragraph copy, e.g. the home page's tagline.
- **Label** (600, 0.75rem `text-xs`, sometimes down to `text-[10px]`): form field labels, metadata
  lines, status badges — almost always paired with a `text-muted`/`text-faint` color.
- **Mono** (400, 0.75rem, `font-mono`): event slugs and filesystem paths in the admin events list —
  the one place monospace signals "this is a literal identifier, copy it exactly."

### Named Rules
**The One Voice Rule.** There is exactly one font family in this system. A new surface never
introduces a second typeface for "contrast" — hierarchy comes from weight, size, and opacity only.

## Layout

Mobile-first and single-column by default; the admin screens are the widest surfaces and still cap
at `max-w-3xl`, centered with `px-4 sm:px-6 py-8`. Guest-facing screens (home, kiosk, guest photo,
login) go narrower still (`max-w-xs`–`max-w-md`) and are vertically centered
(`flex min-h-screen flex-col items-center justify-center`) — each is a single-purpose moment, not a
page to scroll.

The one deliberately non-linear layout is the live wall: a zero-dependency CSS-columns masonry
(`columns-2 sm:columns-3 md:columns-4 lg:columns-5`, `break-inside-avoid`) that scales tile count
with viewport instead of a fixed grid, because photo aspect ratios vary and a rigid grid would crop
or letterbox them.

Admin list/dashboard sections stack in a single column of `rounded-lg` cards
(`flex flex-col gap-3`/`gap-4`), with `grid grid-cols-1 sm:grid-cols-2/3` reserved specifically for
side-by-side stat tiles and reports. Spacing steps in practice: `gap-2`/`p-3` (tight, form rows and
list items), `gap-3`–`gap-4`/`p-4` (default card padding and section rhythm), `gap-6`/`px-6`
(page-level breathing room), `gap-8`/`py-8` (centered single-focus screens).

## Elevation & Depth

Flat by default, everywhere that isn't a photograph. Cards, panels, inputs, and buttons carry no
`box-shadow` at all — depth between surfaces is conveyed purely by tonal layering (`white/10` over
`void-black`, `black/20` nested inside that). The only two places shadow or blur appear are: a
rendered photo itself (`shadow-2xl shadow-black/50` or `/60`, on the kiosk hero image, the guest
photo, and every masonry/lightbox photo), and the lightbox's full-screen backdrop
(`bg-black/80 backdrop-blur-sm`) plus its floating close button (`backdrop-blur`). Depth is a
property of the photograph, not of the interface around it.

### Named Rules
**The Flat Room Rule.** UI chrome never casts a shadow. If something needs to look lifted off the
page, it is a photograph (or the overlay holding one) — not a button, card, or panel.

## Shapes

Two radii carry the whole system, deliberately kept apart: **fully rounded** (`rounded-full`,
9999px) is exclusive to interactive controls — every button, badge, and the home page's search
input — signaling "tap me." **`rounded-lg`** (0.5rem/8px) is exclusive to non-interactive containers
— cards, sections, most form fields. A third, custom radius — **`rounded-photo`** (0.75rem/12px) —
exists solely for photographic imagery (the kiosk/guest/lightbox/wall photos and the QR code image),
slightly more generous than the container radius so a photo never reads as "just another panel."
No borders are used anywhere in the system; surfaces are separated by opacity and radius alone.

## Components

### Buttons
- **Shape:** always `rounded-full`; ghost/ text-only variants exist for tertiary links (e.g.
  "+ Registrar anticipo") and drop the pill background entirely.
- **Primary:** Flash Magenta Deep fill (`#db2777` — the shade that passes AA contrast with white
  text; never the brighter Flash Magenta here), white bold text, `px-4/5/6 py-1.5/2/3` depending on
  context (bigger on guest-facing share/download actions, tighter in dense admin forms).
- **Secondary:** Frosted Panel fill, `text-white/70`, used for every non-primary navigation pill and
  action ("Eventos", "Agenda", "Cancelar").
- **Download (signature variant):** Paper White fill with void-black text — the one button styled as
  a physical object rather than UI, reserved for the actual "save this photo" action.
- **Disabled:** `opacity-30` on the fill, no separate disabled palette.
- **Press feedback:** `active:scale-95` (guest-facing) or `active:scale-[0.98]` (photo tiles) on
  every tappable element — the system's one universal micro-interaction.

### Badges (status)
- **Active/Confirmed:** Flash Magenta Deep fill, white text, `rounded-full px-3 py-1 text-xs font-semibold`
  (same AA-contrast reasoning as the primary button — same fill, same rule).
- **Inactive/Pending:** Frosted Panel fill, `text-white/50` — same shape, quieter fill.

### Cards / Containers
- **Corner Style:** `rounded-lg` (8px), no exceptions.
- **Background:** Frosted Panel (`white/10`) at the top level; **Recessed Well** (`black/20`) for a
  panel nested one level deeper (e.g. the deposits ledger inside an agenda card).
- **Shadow Strategy:** none — see Elevation & Depth.
- **Border:** none.
- **Internal Padding:** `p-4` standard; `p-3` in tighter nested sub-panels.

### Inputs / Fields
- **Style:** Frosted Panel background, `rounded-lg`, no visible border/stroke at rest.
- **Focus:** `ring-2 ring-brand-500` (Flash Magenta) — the ring is the *only* focus/border
  treatment in the system; nothing has a resting border that a focus state thickens.
- **Labels:** small (`text-xs`/`text-sm`) `text-white/70` label sitting directly above the field.

### Navigation
- A flat row of Secondary-style pill links (`rounded-full bg-white/10`) at the top of every admin
  screen, doubling as breadcrumbs between Eventos/Agenda/Finanzas/Inventario — no separate nav
  chrome, no active-state styling on the current section's own link.

### Photo Tile (signature component)
The wall's masonry tile: a native `<button>` (not a div) wrapping a lazy, `@defer`-loaded thumbnail,
with a Polaroid drop-in entrance (`animate-photo-drop` — falls in tilted from a random ±9°, settles
flat over ~0.6s with a slight overshoot) staggered by grid position. It is the system's most
choreographed moment, existing to sell "a photo just landed here," and only plays once per photo
(keyed by id) so realtime arrivals animate in without replaying the whole grid.

## Do's and Don'ts

### Do:
- **Do** ration Flash Magenta to one element per screen — the primary action or the single active
  state. If two things on screen are magenta, one of them is wrong.
- **Do** use `rounded-full` for anything tappable and `rounded-lg` for anything that just contains
  content — never mix the two roles.
- **Do** reserve `rounded-photo` and `shadow-2xl` exclusively for actual photographic imagery.
- **Do** use the four white-opacity text steps (`/90`, `/70`, `/50`, `/30`) as the entire neutral
  text scale — don't introduce a gray hex value.
- **Do** give every tappable element `active:scale-95`-style press feedback.
- **Do** reach for an emoji before reaching for an SVG icon; it's the established icon system.
- **Do** give every icon-only button (no visible text label, e.g. a bare 🗑️) an explicit
  `aria-label` — an emoji is not announced as its meaning by assistive tech, so a label-less emoji
  button has no accessible name at all (confirmed live via an audit).

### Don't:
- **Don't** add a `box-shadow` to a card, button, badge, or input — depth belongs to photographs
  only.
- **Don't** introduce a second accent color; Neon Emerald and Warning Coral are semantic (money
  in/out) only, never decorative.
- **Don't** add a border/stroke to a resting input or card — separation comes from opacity and
  radius, not lines.
- **Don't** build a visually distinct "admin theme" — agenda/finance/inventory use the exact same
  tokens and components as the guest-facing kiosk/wall/guest pages.
- **Don't** introduce a second font family for hierarchy or "personality" — Poppins carries the
  whole system at every weight and size.
