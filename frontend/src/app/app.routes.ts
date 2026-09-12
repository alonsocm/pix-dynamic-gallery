import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/admin/admin-auth.guard';
import { eventResolver } from './core/event/event.resolver';
import { photoResolver } from './core/event/photo.resolver';

export const routes: Routes = [
  {
    // No public "home" experience exists (guests arrive via QR straight into /e/:eventId/... or
    // /kiosk/:eventId) — the bare root now just forwards into the guarded admin area, which
    // itself redirects to /admin/login when there's no session.
    path: '',
    pathMatch: 'full',
    redirectTo: '/admin',
  },
  {
    path: 'admin',
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/admin/admin-login.component').then((m) => m.AdminLoginComponent),
      },
      {
        path: '',
        canActivate: [adminAuthGuard],
        // AdminShellComponent renders the persistent top nav (module links + logout) and its own
        // <router-outlet> for everything below — replaces the pill row every page used to repeat.
        loadComponent: () =>
          import('./features/admin/admin-shell.component').then((m) => m.AdminShellComponent),
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./features/admin/admin-dashboard.component').then((m) => m.AdminDashboardComponent),
          },
          {
            path: 'events',
            loadComponent: () =>
              import('./features/admin/events-list.component').then((m) => m.EventsListComponent),
          },
          {
            // Brought under the guard here (it used to be open) — closes a previously-documented
            // gap for free, since the admin password mechanism is being built anyway.
            path: 'events/new',
            loadComponent: () =>
              import('./features/admin/create-event.component').then((m) => m.CreateEventComponent),
          },
          {
            // :eventId is the slug here too, matching /e/:eventId and /kiosk/:eventId's own
            // convention — reuses the public eventResolver unchanged.
            path: 'events/:eventId/photos',
            resolve: { event: eventResolver },
            loadComponent: () =>
              import('./features/admin/event-photos.component').then((m) => m.EventPhotosComponent),
          },
          {
            // Same :eventId-is-the-slug convention as .../photos above.
            path: 'events/:eventId/finance',
            resolve: { event: eventResolver },
            loadComponent: () =>
              import('./features/admin/event-finance.component').then((m) => m.EventFinanceComponent),
          },
          {
            // Same :eventId-is-the-slug convention as .../photos above.
            path: 'events/:eventId/qr',
            resolve: { event: eventResolver },
            loadComponent: () => import('./features/admin/event-qr.component').then((m) => m.EventQrComponent),
          },
          {
            path: 'agenda',
            loadComponent: () =>
              import('./features/admin/agenda-list.component').then((m) => m.AgendaListComponent),
          },
          {
            path: 'finance',
            loadComponent: () =>
              import('./features/admin/finance-dashboard.component').then((m) => m.FinanceDashboardComponent),
          },
          {
            path: 'inventory',
            loadComponent: () =>
              import('./features/admin/inventory-dashboard.component').then((m) => m.InventoryDashboardComponent),
          },
        ],
      },
    ],
  },
  {
    path: 'kiosk/:eventId',
    resolve: { event: eventResolver },
    loadComponent: () => import('./features/kiosk/kiosk.component').then((m) => m.KioskComponent),
  },
  {
    path: 'e/:eventId',
    resolve: { event: eventResolver },
    children: [
      {
        path: 'p/:photoId',
        resolve: { photo: photoResolver },
        loadComponent: () =>
          import('./features/guest/guest-photo.component').then((m) => m.GuestPhotoComponent),
      },
      {
        path: 'wall',
        loadComponent: () => import('./features/wall/wall.component').then((m) => m.WallComponent),
      },
    ],
  },
  {
    path: 'not-found',
    loadComponent: () =>
      import('./shared/ui/not-found/not-found.component').then((m) => m.NotFoundComponent),
  },
  { path: '**', redirectTo: 'not-found' },
];
