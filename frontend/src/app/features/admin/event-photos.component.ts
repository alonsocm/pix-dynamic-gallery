import { Component, DestroyRef, ElementRef, OnInit, effect, inject, input, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EventDto } from '../../core/models/event.model';
import { AdminPhotoTileComponent } from './admin-photo-tile.component';
import { EventPhotosService } from './event-photos.service';

/**
 * `/admin/events/:eventId/photos` — select one or more photos and delete them. `:eventId` is
 * actually the slug (same convention as `/e/:eventId`), resolved via the existing eventResolver.
 * Infinite-scroll layout mirrors WallComponent's (IntersectionObserver sentinel, Tailwind
 * multi-column masonry) but tiles toggle a selection instead of opening a lightbox.
 */
@Component({
  selector: 'app-event-photos',
  imports: [RouterLink, AdminPhotoTileComponent],
  providers: [EventPhotosService], // route-scoped: fresh state per navigation into this route
  template: `
    <div class="min-h-screen p-4 sm:p-6">
      <div class="mb-6 flex items-center justify-between gap-4">
        <div>
          <a routerLink="/admin/events" class="text-sm text-white/50">← Eventos</a>
          <h1 class="text-2xl font-bold text-white/90">{{ event().name }} — Fotos</h1>
        </div>

        @if (eventPhotos.selectedCount() > 0) {
          <div class="flex items-center gap-3">
            <span class="text-sm text-white/70">{{ eventPhotos.selectedCount() }} seleccionada{{ eventPhotos.selectedCount() === 1 ? '' : 's' }}</span>
            <button
              type="button"
              [disabled]="eventPhotos.deleting()"
              (click)="deleteSelected()"
              class="rounded-full bg-red-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
            >
              {{ eventPhotos.deleting() ? 'Eliminando…' : '🗑️ Eliminar' }}
            </button>
            <button type="button" (click)="eventPhotos.clearSelection()" class="text-sm text-white/50 underline">
              Cancelar
            </button>
          </div>
        }
      </div>

      @if (eventPhotos.photos().length > 0) {
        <div class="columns-2 gap-4 sm:columns-3 md:columns-4 lg:columns-5">
          @for (photo of eventPhotos.photos(); track photo.id) {
            <app-admin-photo-tile
              [photo]="photo"
              [selected]="eventPhotos.selectedIds().has(photo.id)"
              (toggle)="eventPhotos.toggleSelected(photo.id)"
            />
          }
        </div>
      } @else if (!eventPhotos.loading()) {
        <p class="py-16 text-center text-white/50">Este evento todavía no tiene fotos.</p>
      }

      <div #sentinel class="h-4"></div>

      @if (eventPhotos.loading()) {
        <p class="py-4 text-center text-sm text-white/40">Cargando más fotos…</p>
      }

      @if (!eventPhotos.hasNextPage() && eventPhotos.photos().length > 0) {
        <p class="py-4 text-center text-sm text-white/30">Eso es todo ✨</p>
      }
    </div>
  `,
})
export class EventPhotosComponent implements OnInit {
  readonly event = input.required<EventDto>();

  protected readonly eventPhotos = inject(EventPhotosService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly sentinel = viewChild<ElementRef<HTMLElement>>('sentinel');

  constructor() {
    effect(() => {
      const el = this.sentinel();
      if (!el) {
        return;
      }

      const observer = new IntersectionObserver((entries) => {
        if (entries[0]?.isIntersecting) {
          void this.eventPhotos.loadNextPage();
        }
      });
      observer.observe(el.nativeElement);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  ngOnInit(): void {
    void this.eventPhotos.loadInitial(this.event().id);
  }

  protected deleteSelected(): void {
    const count = this.eventPhotos.selectedCount();
    const confirmed = confirm(
      `¿Eliminar ${count} foto${count === 1 ? '' : 's'}? Esta acción no se puede deshacer.`,
    );
    if (confirmed) {
      void this.eventPhotos.deleteSelected();
    }
  }
}
