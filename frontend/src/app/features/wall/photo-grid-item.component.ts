import { Component, computed, input, output } from '@angular/core';
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
 *
 * Entrance is a Polaroid/photo-booth drop-in (`.animate-photo-drop`, keyframes in styles.css):
 * each tile falls in tilted and settles flat. `[index]` staggers the delay on initial load/
 * pagination; the tilt angle is derived from `photo().id` so it's stable across change detection
 * (no re-randomizing on every CD run) and consistent for a given photo. Because the wall `@for`s
 * `track photo.id`, this component only mounts — and thus only animates — once per photo, so a
 * realtime-prepended photo animates in on its own without replaying the whole grid.
 */
@Component({
  selector: 'app-photo-grid-item',
  host: {
    class: 'mb-4 block break-inside-avoid animate-photo-drop',
    '[style.--photo-delay.ms]': 'delayMs()',
    '[style.--tilt-from.deg]': 'tiltDeg()',
  },
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
  /** Position within the current batch — 0 for a lone realtime arrival. Drives the stagger delay. */
  readonly index = input<number>(0);

  /** Capped so a long initial batch/page doesn't leave the last tiles waiting ages to start. */
  protected readonly delayMs = computed(() => Math.min(this.index() * 40, 480));

  /** A small pseudo-random tilt (-9°..9°) derived from the photo id, so it's the same every time
   *  this exact photo renders instead of jumping around on each change-detection pass. */
  protected readonly tiltDeg = computed(() => {
    const id = this.photo().id;
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
      hash = (hash * 31 + id.charCodeAt(i)) | 0;
    }
    return (Math.abs(hash) % 19) - 9;
  });
}
