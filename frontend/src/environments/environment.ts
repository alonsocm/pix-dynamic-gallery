// Fallback used only when window.__env (public/env.js) doesn't provide a value — i.e. plain
// `ng serve` / non-Docker local development. See core/config/app-config.service.ts.
export const environment = {
  apiBaseUrl: 'http://localhost:8080',
  hubBaseUrl: 'http://localhost:8080',
};
