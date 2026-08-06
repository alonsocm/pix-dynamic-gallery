import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, isDevMode } from '@angular/core';
import { provideRouter, withComponentInputBinding, withRouterConfig } from '@angular/router';

import { routes } from './app.routes';
import { provideServiceWorker } from '@angular/service-worker';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(),
    provideRouter(
      routes,
      // withComponentInputBinding(): resolved route data (EventDto/PhotoDto) binds straight to
      // component `input()`s — no ActivatedRoute.data boilerplate in kiosk/guest/wall components.
      withComponentInputBinding(),
      // paramsInheritanceStrategy: 'always': the `e/:eventId` parent route resolves `event` once;
      // its `wall` and `p/:photoId` children need that same resolved data bound to their own
      // `event` input too. Default Angular behavior only inherits parent data on empty-path
      // (layout-only) parents, which `e/:eventId` isn't — this opts in explicitly.
      withRouterConfig({ paramsInheritanceStrategy: 'always' }),
    ),
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
