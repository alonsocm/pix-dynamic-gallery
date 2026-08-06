import { Routes } from '@angular/router';
import { eventResolver } from './core/event/event.resolver';
import { photoResolver } from './core/event/photo.resolver';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent),
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
