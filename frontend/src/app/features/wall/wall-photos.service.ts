import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { PhotoDto, PhotoStatus } from '../../core/models/photo.model';
import { PhotoUploadedNotification } from '../../core/models/signalr-notifications.model';

const PAGE_SIZE = 30;

/**
 * Route-scoped (registered as a component provider on WallComponent, NOT providedIn: 'root') so
 * state resets cleanly every time a user navigates into a fresh event's wall. Owns the one array
 * that both the initial paginated REST fetch and realtime SignalR pushes write into — this is
 * intentionally a plain signal service rather than NgRx: one array, two write paths (append page,
 * prepend realtime), both deduped by id. That's not enough complexity to justify
 * actions/reducers/effects/selectors ceremony for an MVP.
 */
@Injectable()
export class WallPhotosService {
  private readonly api = inject(ApiClient);

  private eventId = '';
  private readonly seenIds = new Set<string>();

  readonly photos = signal<PhotoDto[]>([]);
  readonly pageNumber = signal(0);
  readonly hasNextPage = signal(true);
  readonly loading = signal(false);

  async loadInitial(eventId: string): Promise<void> {
    this.eventId = eventId;
    this.photos.set([]);
    this.seenIds.clear();
    this.pageNumber.set(0);
    this.hasNextPage.set(true);
    await this.loadNextPage();
  }

  async loadNextPage(): Promise<void> {
    if (this.loading() || !this.hasNextPage()) {
      return;
    }

    this.loading.set(true);
    try {
      const result = await firstValueFrom(this.api.getEventPhotos(this.eventId, this.pageNumber() + 1, PAGE_SIZE));
      this.pageNumber.set(result.pageNumber);
      this.hasNextPage.set(result.hasNextPage);

      const freshItems = result.items.filter((photo) => !this.seenIds.has(photo.id));
      freshItems.forEach((photo) => this.seenIds.add(photo.id));

      // Known, accepted MVP tradeoff: offset pagination + concurrent inserts during an active
      // event can rarely skip/duplicate an item right at a page boundary. Dedupe-by-id (above)
      // makes duplication harmless; skipping is a rare cosmetic edge case — fixing it properly
      // would need backend keyset pagination, out of scope here.
      this.photos.update((list) => [...list, ...freshItems]);
    } finally {
      this.loading.set(false);
    }
  }

  /**
   * Builds a minimal PhotoDto straight from the slim SignalR payload — no extra per-photo fetch,
   * for snappiness. `fileName`/`sizeBytes`/`contentType` are blank on realtime-injected tiles
   * since the wall only ever renders `url` (+ timestamp), never those other fields.
   */
  prependRealtime(notification: PhotoUploadedNotification): void {
    if (this.seenIds.has(notification.photoId)) {
      return;
    }
    this.seenIds.add(notification.photoId);

    const photo: PhotoDto = {
      id: notification.photoId,
      eventId: notification.eventId,
      fileName: '',
      url: notification.url,
      contentType: '',
      sizeBytes: 0,
      status: PhotoStatus.Uploaded,
      capturedAtUtc: notification.timestamp,
      uploadedAtUtc: notification.timestamp,
    };

    this.photos.update((list) => [photo, ...list]);
  }
}
