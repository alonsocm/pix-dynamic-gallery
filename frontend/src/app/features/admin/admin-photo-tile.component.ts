import { Component, input, output } from '@angular/core';
import { PhotoDto } from '../../core/models/photo.model';

/**
 * Checkbox-select variant of the wall's PhotoGridItemComponent — deliberately a separate
 * component rather than extending that one, so the already-shipped, production-verified wall
 * lightbox feature stays completely untouched. Same `@defer (on viewport)` lazy-render pattern.
 */
@Component({
  selector: 'app-admin-photo-tile',
  host: { class: 'mb-4 block break-inside-avoid' },
  template: `
    <button
      type="button"
      (click)="toggle.emit()"
      class="relative block w-full touch-manipulation cursor-pointer overflow-hidden rounded-photo text-left transition active:scale-[0.98]"
    >
      @defer (on viewport; prefetch on idle) {
        <img [src]="photo().url" [alt]="photo().fileName || 'Foto del evento'" loading="lazy" class="w-full rounded-photo" />
      } @placeholder {
        <div class="aspect-square w-full animate-pulse rounded-photo bg-white/10"></div>
      } @loading (minimum 100ms) {
        <div class="aspect-square w-full animate-pulse rounded-photo bg-white/10"></div>
      } @error {
        <div class="flex aspect-square w-full items-center justify-center rounded-photo bg-white/5 text-2xl text-white/40">
          ⚠️
        </div>
      }

      <span
        class="absolute right-2 top-2 flex h-6 w-6 items-center justify-center rounded-full border-2 text-sm font-bold"
        [class.bg-brand-500]="selected()"
        [class.border-brand-500]="selected()"
        [class.border-white/70]="!selected()"
        [class.bg-black/40]="!selected()"
      >
        @if (selected()) {
          ✓
        }
      </span>

      @if (selected()) {
        <div class="pointer-events-none absolute inset-0 rounded-photo ring-2 ring-inset ring-brand-500"></div>
      }
    </button>
  `,
})
export class AdminPhotoTileComponent {
  readonly photo = input.required<PhotoDto>();
  readonly selected = input(false);
  readonly toggle = output<void>();
}
