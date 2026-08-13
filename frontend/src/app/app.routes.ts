import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/admin/admin-auth.guard';
import { eventResolver } from './core/event/event.resolver';
import { photoResolver } from './core/event/photo.resolver';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
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
        children: [
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
