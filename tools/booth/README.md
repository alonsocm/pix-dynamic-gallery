# Cabin PC setup (production)

Instructions for the always-on Windows PC that sits inside the photobooth, running the API +
watcher natively and exposing them to the internet via `cloudflared`. This is the production
counterpart to `docker compose up` in the repo root — no Docker here, because
`SparkboothWatcherService` needs to see the real Windows filesystem where Sparkbooth saves
captures, and `FileSystemWatcher` only gets reliable, instant events when it isn't going through a
Docker Desktop/WSL2 bind mount (see the root `docker-compose.yml` comment for why).

You do **not** need to clone this whole repo onto the cabin PC — just the published API output and
three small files from this folder.

## One-time setup

### 1. Install prerequisites

- **.NET 9 Runtime** (not the SDK) — [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download/dotnet/9.0), "ASP.NET Core Runtime" installer for Windows x64.
- **cloudflared** — [github.com/cloudflare/cloudflared/releases](https://github.com/cloudflare/cloudflared/releases), the Windows `.msi`. After install, confirm it's on PATH:
  ```powershell
  cloudflared --version
  ```

### 2. Publish the API and copy it to the cabin PC

From a dev machine with the .NET SDK and this repo cloned:

```powershell
dotnet publish src/PixDynamicGallery.Api -c Release -o publish-output
```

Copy the contents of `publish-output\` to the cabin PC, into a folder named `api` next to
`start-booth.ps1`. If you're setting this up directly from a clone on the cabin PC itself (simpler,
skips the copy step), just run the same `dotnet publish` command with
`-o tools\booth\api` and you're already in the right place.

End state on the cabin PC:

```
tools\booth\
├── start-booth.ps1
├── start-booth.cmd
├── env.production.example
├── .env.production          <- you create this, step 3
└── api\
    ├── PixDynamicGallery.Api.dll
    └── ...
```

### 3. Fill in the secrets

Copy `env.production.example` to `.env.production` (same folder) and fill in every `REPLACE_ME`.
The comments in that file say exactly which Cloudflare/Neon dashboard screen each value comes
from — all of it was already generated in Paso 2 (Neon connection string, R2 bucket credentials,
Cloudflare Tunnel token). `.env.production` is gitignored — it never gets committed, and isn't part
of what you copy from the dev machine.

### 4. Run it

Double-click `start-booth.cmd`. It forces `-ExecutionPolicy Bypass`, so it works regardless of
what "Run with PowerShell" or the machine's configured execution policy would otherwise do (that
context-menu action only bypasses the policy conditionally, and can silently fail to run the
script on a cabin PC with a stricter policy). From a terminal, the equivalent is:

```powershell
powershell -ExecutionPolicy Bypass -File tools\booth\start-booth.ps1
```

This loads `.env.production` into the environment and starts two windows: the API (`dotnet
PixDynamicGallery.Api.dll`) and `cloudflared tunnel run`. Leave both open — closing either stops
that half of the stack. Once both are up, `https://api.somospix.com` should answer (try
`https://api.somospix.com/swagger` from any device, not just the cabin PC, to confirm the tunnel is
actually working end to end).

Make this run automatically at Windows startup by putting a shortcut to the command above in the
Startup folder (`shell:startup`), so the booth comes back online on its own after a reboot/power
cut at a venue.

## Kiosk screen setup

The kiosk screen (the one guests see, with the live photo + QR code) is just a browser pointed at
`https://somospix.com/kiosk/<eventId>`, in full-screen/kiosk mode so there's no address bar or
window chrome to fiddle with:

- **Chrome/Edge**: `chrome.exe --kiosk https://somospix.com/kiosk/<eventId>` (a desktop shortcut
  with that target is the simplest way to make it a one-click launch).
- Disable sleep/screensaver on the display (Windows Settings > Power) — the screen needs to stay on
  for the whole event.
- To exit kiosk mode for maintenance: `Alt+F4` (Chrome/Edge) closes the window.

## Troubleshooting

- **Tunnel shows "Inactive" in the Cloudflare dashboard** — normal until `cloudflared` is actually
  running (step 4). If it stays inactive after that, check the `cloudflared` window for connection
  errors (firewall blocking outbound, wrong token).
- **API starts but photos don't upload** — check the API window's console output; the watcher logs
  every file it picks up. Confirm the event's `WatchFolderPath` is the real Windows path Sparkbooth
  writes to (native run, unlike Docker, uses the actual Windows path directly, not a container
  path like `/data/sparkbooth`).
- **CORS errors in the browser console** — confirm `Cors__AllowedOrigins` in `.env.production`
  matches the exact origin the frontend is served from (scheme + host, no trailing slash).
