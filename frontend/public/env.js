// Runtime configuration, read by AppConfigService before any Angular code executes.
// Checked-in default for local `ng serve` / non-Docker use: empty values mean "fall back to
// environment.ts". This file is regenerated:
//  - at *build* time by `npm run build`'s `prebuild` hook (scripts/generate-env.mjs), from
//    API_BASE_URL/HUB_BASE_URL env vars — this is what Cloudflare Pages uses (static hosting,
//    no server-side entrypoint, so the URLs must be baked in before deploy).
//  - at *container start* in the Docker image, from the same env vars — see
//    frontend/docker/env.template.js and frontend/docker/docker-entrypoint.d/30-generate-env.sh.
//
// Both regeneration paths overwrite this comment along with the values — that's expected, this
// header is documentation for whoever's reading the repo, not something that needs to survive a
// build.
window.__env = {
  apiBaseUrl: '',
  hubBaseUrl: '',
};
