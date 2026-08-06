import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

interface RuntimeEnv {
  apiBaseUrl?: string;
  hubBaseUrl?: string;
}

declare global {
  interface Window {
    __env?: RuntimeEnv;
  }
}

/**
 * Resolves the API/SignalR base URLs at runtime from `window.__env` (see public/env.js), which
 * the Docker image regenerates at container start from `API_BASE_URL`/`HUB_BASE_URL` env vars
 * (frontend/docker/docker-entrypoint.d/30-generate-env.sh). Falls back to `environment.ts` for
 * plain `ng serve`, where env.js's checked-in defaults are empty strings.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  readonly apiBaseUrl: string = window.__env?.apiBaseUrl || environment.apiBaseUrl;
  readonly hubBaseUrl: string = window.__env?.hubBaseUrl || environment.hubBaseUrl;
}
