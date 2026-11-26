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
 * A corner magnifier (decorative — the button's accessible name is "Ver foto en HD") is the
 * visual cue that the thumbnail is tappable and opens the full photo, not the file itself.
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
      aria-label="Ver foto en HD"
      class="relative block w-full touch-manipulation cursor-pointer rounded-photo text-left transition active:scale-[0.98]"
    >
      @defer (on viewport; prefetch on idle) {
        <img [src]="thumbSrc()" [alt]="photo().fileName || 'Foto del evento'" loading="lazy" class="w-full rounded-photo" />
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
        aria-hidden="true"
        class="pointer-events-none absolute right-2 bottom-2 flex h-8 w-8 items-center justify-center rounded-full bg-black/60 text-white shadow-md ring-1 ring-white/30 sm:h-9 sm:w-9"
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2.25"
          stroke-linecap="round"
          stroke-linejoin="round"
          class="h-4 w-4"
        >
          <circle cx="11" cy="11" r="6.5" />
          <path d="m20 20-3.2-3.2" />
        </svg>
      </span>
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

  /**
   * Grid tiles render the small server-generated preview instead of the full-size original —
   * that's most of the wall's network weight for an event with many photos. Falls back to the
   * original whenever no thumbnail exists yet (generation failed, or a photo just arrived over
   * SignalR and hasn't been through a page load yet — see WallPhotosService.prependRealtime).
   */
  protected readonly thumbSrc = computed(() => this.photo().thumbnailUrl ?? this.photo().url);

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
