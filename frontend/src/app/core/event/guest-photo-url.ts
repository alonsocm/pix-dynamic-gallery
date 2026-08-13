import { EventDto } from '../models/event.model';

/** Mirrors Event.BuildGuestPhotoUrl on the backend exactly. */
export function buildGuestPhotoUrl(event: EventDto, photoId: string): string {
  return `${event.guestBaseUrl}/e/${event.slug}/p/${photoId}`;
}

/** Mirrors Event.BuildGuestWallUrl on the backend exactly. */
export function buildGuestWallUrl(event: EventDto): string {
  return `${event.guestBaseUrl}/e/${event.slug}/wall`;
}
