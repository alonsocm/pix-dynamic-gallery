// Fallback used only when window.__env (public/env.js) doesn't provide a value — i.e. plain
// `ng serve` / non-Docker local development. See core/config/app-config.service.ts.
//
// apiBaseUrl points at a local `pix-app` (Cloudflare Worker, separate repo D:\src\pix-app) run via
// `npm run dev` (wrangler dev, default port 8787) — that repo now owns event/photo reads, agenda,
// finance and inventory. hubBaseUrl stays on the local cabin API (this repo, docker-compose/`dotnet
// run`) — it's the only thing this repo still serves to the frontend.
export const environment = {
  apiBaseUrl: 'http://localhost:8787',
  hubBaseUrl: 'http://localhost:8080',
};
