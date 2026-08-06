// Runtime configuration, read by AppConfigService before any Angular code executes.
// Checked-in default for local `ng serve` / non-Docker use: empty values mean "fall back to
// environment.ts". In the Docker image this file is regenerated at container start from
// API_BASE_URL/HUB_BASE_URL env vars — see frontend/docker/env.template.js and
// frontend/docker/docker-entrypoint.d/30-generate-env.sh.
window.__env = {
  apiBaseUrl: '',
  hubBaseUrl: '',
};
