import { Component, ElementRef, afterNextRender, inject, input, output } from '@angular/core';
import { EventDto } from '../../core/models/event.model';
import { PhotoDto } from '../../core/models/photo.model';
import { DownloadShareButtonsComponent } from '../../shared/ui/download-share-buttons/download-share-buttons.component';

/**
 * Full-screen overlay opened from the wall when a guest taps a tile — the photo at full size plus
 * the same download/share actions as the guest page. Deliberately not a route: it reuses the
 * `PhotoDto` the wall already has in memory, no refetch, no deep link.
 *
 * No QR here (for now — events currently point a physical QR straight at `/e/:eventId/wall`
 * instead of running a dedicated wall screen, so a per-photo QR isn't needed).
 *
 * Layout is flex-based, not a fixed `dvh` percentage, so the photo and the buttons never fight
 * over space: the host stretches its content div to the full viewport height, the photo's wrapper
 * is `flex-1 min-h-0` (absorbs whatever space is left over and is allowed to shrink below its
 * content size — the `min-h-0` is what actually prevents overflow/scroll), and the buttons are
 * `shrink-0` so they always render at full, tappable size — any squeeze lands on the photo, never
 * on the buttons.
 */
@Component({
  selector: 'app-photo-lightbox',
  imports: [DownloadShareButtonsComponent],
  host: {
    class: 'fixed inset-0 z-50 flex justify-center overflow-y-auto overscroll-contain bg-black/80 p-4 backdrop-blur-sm',
    role: 'dialog',
    'aria-modal': 'true',
    'aria-label': 'Foto ampliada',
    tabindex: '-1',
    '(click)': 'close.emit()',
    '(document:keydown.escape)': 'close.emit()',
  },
  template: `
    <div
      class="relative flex h-full w-full max-w-3xl flex-col items-center gap-4"
      (click)="$event.stopPropagation()"
    >
      <button
        type="button"
        (click)="close.emit()"
        aria-label="Cerrar"
        class="absolute -top-3 right-0 z-10 flex h-11 w-11 touch-manipulation items-center justify-center rounded-full bg-white/10 text-xl text-white/80 backdrop-blur transition hover:bg-white/20"
      >
        ✕
      </button>

      <div class="flex min-h-0 w-full flex-1 items-center justify-center">
        @if (photo().url; as url) {
          <img
            [src]="url"
            [alt]="photo().fileName || event().name"
            class="max-h-full max-w-full rounded-photo object-contain shadow-2xl shadow-black/50"
          />
        }
      </div>

      @if (photo().url; as url) {
        <app-download-share-buttons [url]="url" [title]="event().name" class="w-full max-w-md shrink-0" />
      }
    </div>
  `,
})
export class PhotoLightboxComponent {
  readonly photo = input.required<PhotoDto>();
  readonly event = input.required<EventDto>();
  readonly close = output<void>();

  constructor() {
    const host = inject(ElementRef<HTMLElement>);
    afterNextRender(() => host.nativeElement.focus());
  }
}
