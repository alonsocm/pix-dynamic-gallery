import { Component, ElementRef, afterNextRender, computed, inject, input, output } from '@angular/core';
import { buildGuestPhotoUrl } from '../../core/event/guest-photo-url';
import { EventDto } from '../../core/models/event.model';
import { PhotoDto } from '../../core/models/photo.model';
import { DownloadShareButtonsComponent } from '../../shared/ui/download-share-buttons/download-share-buttons.component';
import { QrCodeComponent } from '../../shared/ui/qr-code/qr-code.component';

/**
 * Full-screen overlay opened from the wall when a guest taps a tile — the photo at full size,
 * its personal QR code (same URL a scan of the physical kiosk QR would produce), and the same
 * download/share actions as the guest page. Deliberately not a route: it reuses the `PhotoDto`
 * the wall already has in memory, no refetch, no deep link.
 *
 * `max-h-[70dvh]` (not `vh`) matters on mobile: `vh` is sized against the "large" viewport before
 * the browser's address bar collapses, so on first paint the photo can render taller than what's
 * actually visible. `dvh` tracks the real, current viewport instead. `overscroll-contain` stops a
 * scroll inside this overlay (e.g. short landscape phones where content doesn't fit) from
 * chaining into a rubber-band scroll of the wall behind it.
 */
@Component({
  selector: 'app-photo-lightbox',
  imports: [QrCodeComponent, DownloadShareButtonsComponent],
  host: {
    class:
      'fixed inset-0 z-50 flex items-center justify-center overflow-y-auto overscroll-contain bg-black/80 p-4 backdrop-blur-sm',
    role: 'dialog',
    'aria-modal': 'true',
    'aria-label': 'Foto ampliada',
    tabindex: '-1',
    '(click)': 'close.emit()',
    '(document:keydown.escape)': 'close.emit()',
  },
  template: `
    <div
      class="relative flex w-full max-w-3xl flex-col items-center gap-6 rounded-photo"
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

      <div class="grid w-full items-start gap-6 sm:grid-cols-[minmax(0,1fr)_14rem]">
        @if (photo().url; as url) {
          <img
            [src]="url"
            [alt]="photo().fileName || event().name"
            class="max-h-[70dvh] w-full rounded-photo object-contain shadow-2xl shadow-black/50 sm:max-h-[80dvh]"
          />
        }

        <div class="flex flex-col items-center gap-3">
          <app-qr-code [value]="guestUrl()" />
          <p class="text-center text-xs text-white/60">Escanea para ver tu foto en tu celular</p>
        </div>
      </div>

      @if (photo().url; as url) {
        <app-download-share-buttons [url]="url" [title]="event().name" class="w-full max-w-md" />
      }
    </div>
  `,
})
export class PhotoLightboxComponent {
  readonly photo = input.required<PhotoDto>();
  readonly event = input.required<EventDto>();
  readonly close = output<void>();

  protected readonly guestUrl = computed(() => buildGuestPhotoUrl(this.event(), this.photo().id));

  constructor() {
    const host = inject(ElementRef<HTMLElement>);
    afterNextRender(() => host.nativeElement.focus());
  }
}
