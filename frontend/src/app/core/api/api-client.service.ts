import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AppConfigService } from '../config/app-config.service';
import { CreateEventRequest, EventDto } from '../models/event.model';
import { PaginatedList } from '../models/paginated-list.model';
import { PhotoDto } from '../models/photo.model';

/** Thin HttpClient wrapper over the REST surface exposed by EventsController/PhotosController. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  /** GET /api/events/{slug} — slug is the human-readable event identifier used throughout the URLs. */
  getEvent(slug: string): Observable<EventDto> {
    return this.http.get<EventDto>(`${this.config.apiBaseUrl}/api/events/${encodeURIComponent(slug)}`);
  }

  /** POST /api/events — used by the admin "create event" screen. */
  createEvent(request: CreateEventRequest): Observable<EventDto> {
    return this.http.post<EventDto>(`${this.config.apiBaseUrl}/api/events`, request);
  }

  /** GET /api/events/{eventId}/photos — eventId here is the real Guid (EventDto.id), not the slug. */
  getEventPhotos(eventId: string, pageNumber = 1, pageSize = 30): Observable<PaginatedList<PhotoDto>> {
    return this.http.get<PaginatedList<PhotoDto>>(`${this.config.apiBaseUrl}/api/events/${eventId}/photos`, {
      params: { pageNumber, pageSize },
    });
  }

  /** GET /api/events/{eventId}/photos/{photoId} — both are Guids. */
  getPhoto(eventId: string, photoId: string): Observable<PhotoDto> {
    return this.http.get<PhotoDto>(`${this.config.apiBaseUrl}/api/events/${eventId}/photos/${photoId}`);
  }
}
