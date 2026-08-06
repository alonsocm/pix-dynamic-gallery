import { HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { RedirectCommand, Router, ResolveFn } from '@angular/router';
import { catchError, of } from 'rxjs';
import { EventDto } from '../models/event.model';
import { EventService } from './event.service';

/**
 * Resolves the `:eventId` route param — which holds the event's **slug**, not its Guid `id`
 * (matches the product's own URL spec and `Event.BuildGuestPhotoUrl`/`BuildGuestWallUrl` in the
 * backend) — to the full `EventDto` before the route activates. Shared by `/kiosk/:eventId` and
 * `/e/:eventId` (and therefore its `wall`/`p/:photoId` children, which inherit it).
 */
export const eventResolver: ResolveFn<EventDto | RedirectCommand> = (route) => {
  const eventService = inject(EventService);
  const router = inject(Router);

  const slug = route.paramMap.get('eventId')!;

  return eventService.resolveBySlug(slug).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 404) {
        return of(new RedirectCommand(router.parseUrl('/not-found')));
      }
      throw error;
    }),
  );
};
