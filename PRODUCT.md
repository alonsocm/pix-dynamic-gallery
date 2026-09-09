# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Two distinct audiences, confirmed by the user:

- **Event guests** — attendees at a photobooth event (wedding, quinceañera, birthday, etc.) who
  never authenticate. They watch the kiosk screen for the live QR, scan it on their own phone to
  land on their photo, download/share it, and browse the live Pinterest-style wall. No technical
  skill assumed; usage happens in the moment, often on venue WiFi or mobile data, sometimes at
  night/low light near the photobooth.
- **The business operator** — the user (Alonso), running the "somospix" photobooth business
  **solo**. Confirmed: no other staff use the admin side today. The operator uses the admin screens
  (agenda/bookings, finance, inventory, event creation/management) to run the business end to end,
  typically before/after events rather than during them.

## Product Purpose

Two jobs under one system:

1. **Real-time event gallery** — watches the local folder where Sparkbooth (photobooth capture
   software) saves each shot, uploads it to cloud storage, and pushes it live over SignalR to a
   kiosk screen (with a dynamic QR) and to every guest's phone (mobile PWA + live wall). Success =
   a guest sees and can save/share their photo within seconds of the shot being taken, with zero
   setup on their end.
2. **Business operations tooling** — agenda/bookings with deposits, per-event and global finance
   tracking, and consumables inventory (photo paper/ink, USB drives) so the operator can run
   somospix as a business without a separate spreadsheet or tool.

## Positioning

Confirmed by the user: **the real-time guest/kiosk gallery experience is the product**; agenda,
finance, and inventory are internal tooling that support running the business behind it, not a
co-equal pillar of the pitch. A neighboring product could not truthfully copy the same mechanism:
watching Sparkbooth's own capture folder and pushing each photo live over SignalR to both a kiosk
QR and every guest's phone, with no app install and no manual upload step.

## Operating Context

- A physical **photobooth cabin PC** (Windows) runs Sparkbooth plus this system's API natively, so
  the `FileSystemWatcher` can see captures on the real local filesystem. This PC is powered on/off
  per event.
- A kiosk screen at the venue displays the live QR (`chrome.exe --kiosk`), often on venue WiFi.
- Guests interact only through their own phones (PWA/live wall), no app install, no login.
- The operator manages events, bookings, money, and consumables inventory from the admin screens,
  normally away from the event itself (office/personal time, not live at the venue).
- Production topology (as of Sept 2026): Cloudflare Pages (frontend), Neon Postgres, Cloudflare R2
  (photo storage), Cloudflare Tunnel (exposes the cabin API), Azure Container Apps (always-on
  read-only API standby so the gallery survives the cabin being powered off between events). See
  `README.md` / `ESTADO_PROYECTO.md` for the full topology and provisioning history.

## Capabilities and Constraints

- Clean Architecture (.NET 9: Domain/Application/Infrastructure/Api) + Angular 22 frontend
  (standalone components, signals, zoneless), Tailwind CSS v4.
- Admin auth is a single shared password (`AdminOptions.Password`, empty = auth disabled) — there
  is no per-user account system; this matches the confirmed solo-operator model, not a gap to fix.
- Real-time delivery is SignalR-only; the guest/wall experience degrades to REST polling on load
  but has no offline mode.
- The watcher requires a real local filesystem, which is why the write-side API cannot fully move
  to the cloud without a separate sync agent (see Operating Context).
- Terminology: "event" = one photobooth booking/session with its own watch folder, slug, and guest
  URL; "wall" = the live Pinterest-style grid of an event's photos; "agenda" = the booking/deposit
  tracker; "finance" = income/expense tracking per event and globally; "inventory" = paper/ink and
  USB drive stock tracking for the printer/handoff consumables.

## Brand Commitments

- Product/brand name: **Pix** (repo, logo wordmark) doing business as **somospix.com** (the brand
  domain). This app (kiosk/wall/admin) lives at **app.somospix.com**; the root `somospix.com` is a
  separate marketing landing page project, not this repo.
- Logo assets exist at `frontend/public/brand/` (`pix-logo-full.png`, `pix-mark.png`,
  `pix-wordmark.png`).
- Admin UI copy is in **Spanish** (confirmed) and should stay that way — no i18n was requested
  there. Guest-facing surface language was not confirmed either way; do not assume English support
  without asking.

## Evidence on Hand

- Real production topology, real bugs found and fixed, and real deploy history are documented in
  `ESTADO_PROYECTO.md` (Spanish) and `README.md` (English) — treat both as authoritative operating
  history, not marketing copy.
- No testimonials, press, case studies, or pricing exist yet; future work must not invent them.
- Real logo/wordmark assets exist (see Brand Commitments) — do not generate placeholder logos.

## Product Principles

1. **The gallery experience is the product.** Guest-facing real-time delivery (kiosk QR → phone,
   live wall) is what somospix is sold on; keep it the top design priority over admin tooling.
2. **Zero friction for guests.** No login, no app install, works from a QR scan on unpredictable
   venue networks — never add a guest-side step that isn't strictly necessary.
3. **Solo-operator admin, not enterprise tooling.** Agenda/finance/inventory serve one person
   running their own business; keep them fast and direct rather than building for teams or roles
   that don't exist yet.
4. **Real filesystem, real venues.** Design and code must account for the cabin PC's local
   watcher, flaky venue connectivity, and the cabin being powered off outside events.
5. **Spanish-first for the operator, evidence-first everywhere.** Admin copy is Spanish; no
   feature ships on invented data, testimonials, or claims not backed by what's actually built.
