import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { PhotoDto } from '../../core/models/photo.model';

const PAGE_SIZE = 60;

/**
 * Route-scoped (component provider on EventPhotosComponent, not providedIn: 'root'), mirrors
 * WallPhotosService's accumulate-on-scroll shape so multi-select survives across pages loaded
 * while scrolling — selection lives in a Set keyed by photo id, independent of how many pages
 * have been fetched so far.
 */
@Injectable()
export class EventPhotosService {
  private readonly api = inject(ApiClient);

  private eventId = '';
  private readonly seenIds = new Set<string>();

  readonly photos = signal<PhotoDto[]>([]);
  readonly pageNumber = signal(0);
  readonly hasNextPage = signal(true);
  readonly loading = signal(false);
  readonly deleting = signal(false);

  readonly selectedIds = signal<ReadonlySet<string>>(new Set());
  readonly selectedCount = computed(() => this.selectedIds().size);

  async loadInitial(eventId: string): Promise<void> {
    this.eventId = eventId;
    this.photos.set([]);
    this.seenIds.clear();
    this.pageNumber.set(0);
    this.hasNextPage.set(true);
    this.selectedIds.set(new Set());
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
      this.photos.update((list) => [...list, ...freshItems]);
    } finally {
      this.loading.set(false);
    }
  }

  toggleSelected(photoId: string): void {
    this.selectedIds.update((current) => {
      const next = new Set(current);
      if (next.has(photoId)) {
        next.delete(photoId);
      } else {
        next.add(photoId);
      }
      return next;
    });
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  /** Deletes the current selection and removes it from the local list — no refetch needed. */
  async deleteSelected(): Promise<void> {
    const ids = [...this.selectedIds()];
    if (ids.length === 0) {
      return;
    }

    this.deleting.set(true);
    try {
      await firstValueFrom(this.api.deletePhotos(this.eventId, ids));
      const deletedSet = new Set(ids);
      ids.forEach((id) => this.seenIds.delete(id));
      this.photos.update((list) => list.filter((photo) => !deletedSet.has(photo.id)));
      this.selectedIds.set(new Set());
    } finally {
      this.deleting.set(false);
    }
  }
}
