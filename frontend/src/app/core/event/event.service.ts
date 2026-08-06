import { Injectable, inject, signal } from '@angular/core';
import { Observable, of, tap } from 'rxjs';
import { ApiClient } from '../api/api-client.service';
import { EventDto } from '../models/event.model';

/**
 * Resolves an event's URL slug to its full `EventDto` (including the real Guid `id` every
 * Photos/SignalR call needs), caches by slug so navigating between `/e/:eventId/wall` and
 * `/e/:eventId/p/:photoId` for the same event doesn't re-fetch, and exposes the most recently
 * resolved event as a signal for anything downstream that doesn't have direct route access.
 */
@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly api = inject(ApiClient);
  private readonly cache = new Map<string, EventDto>();

  readonly currentEvent = signal<EventDto | null>(null);

  resolveBySlug(slug: string): Observable<EventDto> {
    const cached = this.cache.get(slug);
    if (cached) {
      this.currentEvent.set(cached);
      return of(cached);
    }

    return this.api.getEvent(slug).pipe(
      tap((event) => {
        this.cache.set(slug, event);
        this.currentEvent.set(event);
      }),
    );
  }
}
