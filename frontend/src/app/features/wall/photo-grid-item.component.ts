import { Component, input, output } from '@angular/core';
import { PhotoDto } from '../../core/models/photo.model';

/**
 * One masonry tile. `@defer (on viewport)` means offscreen tiles don't pay any rendering/network
 * cost until they scroll into view — Angular sets up the IntersectionObserver internally, no
 * custom code needed here (contrast with WallComponent's own IntersectionObserver, which is for
 * pagination, a different concern). `prefetch on idle` warms the ones just below the fold during
 * idle time so they feel instant once they do scroll in.
 *
 * The tile is a native `<button>` (not a `<div>` with a manual click handler) so tap/click,
 * keyboard activation (Enter/Space) and focus styling all come for free — `touch-manipulation`
 * removes the ~300ms tap delay some mobile browsers still apply to ambiguous double-tap targets.
 */
@Component({
  selector: 'app-photo-grid-item',
  host: { class: 'mb-4 block break-inside-avoid' },
  template: `
    <button
      type="button"
      (click)="select.emit()"
      class="block w-full touch-manipulation cursor-pointer rounded-photo text-left transition active:scale-[0.98]"
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
    </button>
  `,
})
export class PhotoGridItemComponent {
  readonly photo = input.required<PhotoDto>();
  readonly select = output<void>();
}
