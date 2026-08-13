import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminAuthService } from '../admin/admin-auth.service';
import { AppConfigService } from '../config/app-config.service';
import { AdminEventDto, CreateEventRequest, EventDto } from '../models/event.model';
import { PaginatedList } from '../models/paginated-list.model';
import { PhotoDto } from '../models/photo.model';

export interface DeletePhotosResult {
  deletedCount: number;
  notFoundPhotoIds: string[];
}

/** Thin HttpClient wrapper over the REST surface exposed by EventsController/PhotosController. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);
  private readonly adminAuth = inject(AdminAuthService);

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

  /** GET /api/events — admin-only; also doubles as AdminLoginComponent's "verify this password" call. */
  listEvents(): Observable<AdminEventDto[]> {
    return this.http.get<AdminEventDto[]>(`${this.config.apiBaseUrl}/api/events`, { headers: this.adminHeaders() });
  }

  /** PATCH /api/events/{eventId}/active — admin-only. */
  setEventActive(eventId: string, isActive: boolean): Observable<EventDto> {
    return this.http.patch<EventDto>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/active`,
      { isActive },
      { headers: this.adminHeaders() },
    );
  }

  /** POST /api/events/{eventId}/photos/delete — admin-only bulk hard-delete. */
  deletePhotos(eventId: string, photoIds: string[]): Observable<DeletePhotosResult> {
    return this.http.post<DeletePhotosResult>(
      `${this.config.apiBaseUrl}/api/events/${eventId}/photos/delete`,
      { photoIds },
      { headers: this.adminHeaders() },
    );
  }

  private adminHeaders(): HttpHeaders {
    const password = this.adminAuth.password();
    return password ? new HttpHeaders({ 'X-Admin-Password': password }) : new HttpHeaders();
  }
}
