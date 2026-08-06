import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, Router, ResolveFn } from '@angular/router';
import { catchError, of } from 'rxjs';
import { ApiClient } from '../api/api-client.service';
import { PhotoDto } from '../models/photo.model';
import { EventService } from './event.service';

/**
 * Resolves `:photoId` for the guest landing page (`/e/:eventId/p/:photoId`). Relies on the
 * parent `e/:eventId` route's `eventResolver` having already run and populated
 * `EventService.currentEvent` — Angular resolves parent route data before child route data, so
 * this is safe by construction.
 */
export const photoResolver: ResolveFn<PhotoDto | RedirectCommand> = (route) => {
  const api = inject(ApiClient);
  const eventService = inject(EventService);
  const router = inject(Router);

  const photoId = route.paramMap.get('photoId')!;
  const event = eventService.currentEvent();

  if (!event) {
    // Should be unreachable (the parent resolver always runs first and would itself have
    // redirected on failure) — defensive fallback rather than throwing into the router.
    return of(new RedirectCommand(router.parseUrl('/not-found')));
  }

  return api.getPhoto(event.id, photoId).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 404) {
        return of(new RedirectCommand(router.parseUrl('/not-found')));
      }
      throw error;
    }),
  );
};
