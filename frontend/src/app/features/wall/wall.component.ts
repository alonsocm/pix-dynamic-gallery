import { Component, DestroyRef, ElementRef, OnInit, effect, inject, input, signal, viewChild } from '@angular/core';
import { EventDto } from '../../core/models/event.model';
import { PhotoDto } from '../../core/models/photo.model';
import { SignalRService } from '../../core/signalr/signalr.service';
import { PhotoGridItemComponent } from './photo-grid-item.component';
import { PhotoLightboxComponent } from './photo-lightbox.component';
import { WallPhotosService } from './wall-photos.service';

/**
 * `/e/:eventId/wall` — the Pinterest/mosaic-style live wall. Initial paginated REST fetch merged
 * with realtime `OnPhotoUploaded` pushes (see WallPhotosService), infinite-scrolled via an
 * IntersectionObserver sentinel, laid out with zero-dependency Tailwind CSS multi-column masonry
 * (native CSS Grid `masonry` isn't broadly supported yet, so no JS masonry library is needed
 * either way).
 *
 * Guests were long-pressing the grid thumbnails because nothing told them a tap opens the photo
 * (and the HD download). The hint lives in a sticky header band — pinned on a phone and on a
 * projector, inset by `env(safe-area-inset-*)` (needs `viewport-fit=cover`) — with the masonry
 * starting underneath it, so the chip never sits on top of a photo.
 */
@Component({
  selector: 'app-wall',
  imports: [PhotoGridItemComponent, PhotoLightboxComponent],
  providers: [WallPhotosService], // route-scoped: a fresh instance (and fresh state) per navigation into this route
  template: `
    <header
      class="sticky top-0 z-20 border-b border-white/10 bg-ink-900 pt-[max(0.75rem,env(safe-area-inset-top))] pr-[max(1rem,env(safe-area-inset-right))] pb-3 pl-[max(1rem,env(safe-area-inset-left))] sm:pr-[max(1.5rem,env(safe-area-inset-right))] sm:pb-4 sm:pl-[max(1.5rem,env(safe-area-inset-left))]"
    >
      <div class="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3">
        <img src="brand/pix-wordmark.png" alt="PIX" class="h-7 w-auto opacity-80 sm:h-9" />
        <h1 class="text-center text-lg leading-tight font-bold text-white/90 sm:text-2xl">
          {{ event().name }} — Muro en vivo
        </h1>
        <!-- Same asset, hidden, so the title stays optically centered beside the wordmark. -->
        <img src="brand/pix-wordmark.png" alt="" aria-hidden="true" class="invisible h-7 w-auto sm:h-9" />
      </div>

      <p
        role="note"
        class="mx-auto mt-3 flex w-fit max-w-full items-center justify-center gap-2 rounded-full bg-brand-600 px-4 py-2 text-center text-sm leading-snug font-semibold text-white shadow-md sm:px-5 sm:text-base lg:px-6 lg:py-2.5 lg:text-xl"
      >
        <svg
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2.25"
          stroke-linecap="round"
          stroke-linejoin="round"
          aria-hidden="true"
          class="h-4 w-4 shrink-0 sm:h-5 sm:w-5 lg:h-6 lg:w-6"
        >
          <circle cx="11" cy="11" r="6.5" />
          <path d="m20 20-3.2-3.2" />
        </svg>
        Toca tu foto para verla y descargarla en HD
      </p>
    </header>

    <div
      class="min-h-screen pt-4 pr-[max(1rem,env(safe-area-inset-right))] pb-[max(1rem,env(safe-area-inset-bottom))] pl-[max(1rem,env(safe-area-inset-left))] sm:pr-[max(1.5rem,env(safe-area-inset-right))] sm:pb-[max(1.5rem,env(safe-area-inset-bottom))] sm:pl-[max(1.5rem,env(safe-area-inset-left))]"
    >
      @if (wallPhotos.photos().length > 0) {
        <div class="columns-2 gap-4 sm:columns-3 md:columns-4 lg:columns-5">
          @for (photo of wallPhotos.photos(); track photo.id; let i = $index) {
            <app-photo-grid-item [photo]="photo" [index]="i" (select)="openLightbox(photo)" />
          }
        </div>
      } @else if (!wallPhotos.loading()) {
        <p class="py-16 text-center text-white/50">Aún no hay fotos. ¡Sé el primero en tomarte una! 📸</p>
      }

      <div #sentinel class="h-4"></div>

      @if (wallPhotos.loading()) {
        <p class="py-4 text-center text-sm text-white/40">Cargando más fotos…</p>
      }

      @if (!wallPhotos.hasNextPage() && wallPhotos.photos().length > 0) {
        <p class="py-4 text-center text-sm text-white/30">Eso es todo por ahora ✨</p>
      }
    </div>

    @if (selectedPhoto(); as photo) {
      <app-photo-lightbox [photo]="photo" [event]="event()" (close)="closeLightbox()" />
    }
  `,
})
export class WallComponent implements OnInit {
  readonly event = input.required<EventDto>();

  protected readonly wallPhotos = inject(WallPhotosService);
  private readonly signalR = inject(SignalRService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly sentinel = viewChild<ElementRef<HTMLElement>>('sentinel');

  protected readonly selectedPhoto = signal<PhotoDto | null>(null);
  /** Scroll position saved while the lightbox's body-lock is active, restored on close. */
  private scrollY = 0;

  constructor() {
    // Belt-and-braces cleanup in case the user navigates away (e.g. browser back) while the
    // lightbox is open — otherwise the body would stay locked in place on the next page.
    this.destroyRef.onDestroy(() => this.unlockBodyScroll());

    // Realtime merge: every new global lastPhotoUploaded() push gets filtered to this event and
    // prepended. prependRealtime writes wallPhotos.photos; effect() allows that write directly.
    effect(() => {
      const notification = this.signalR.lastPhotoUploaded();
      if (notification && notification.eventId === this.event().id) {
        this.wallPhotos.prependRealtime(notification);
      }
    });

    // Pagination: once the sentinel element exists in the DOM (after first render), watch it and
    // load the next page whenever it scrolls into view.
    effect(() => {
      const el = this.sentinel();
      if (!el) {
        return;
      }

      const observer = new IntersectionObserver((entries) => {
        if (entries[0]?.isIntersecting) {
          void this.wallPhotos.loadNextPage();
        }
      });
      observer.observe(el.nativeElement);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  // Required inputs aren't readable in the constructor (Angular binds them right after
  // construction, before ngOnInit) — initial load + SignalR join/leave lives here instead.
  ngOnInit(): void {
    const eventId = this.event().id;
    void this.wallPhotos.loadInitial(eventId);
    void this.signalR.joinEvent(eventId);
    this.destroyRef.onDestroy(() => void this.signalR.leaveEvent(eventId));
  }

  protected openLightbox(photo: PhotoDto): void {
    this.selectedPhoto.set(photo);
    this.lockBodyScroll();
  }

  protected closeLightbox(): void {
    this.selectedPhoto.set(null);
    this.unlockBodyScroll();
  }

  /**
   * `overflow: hidden` on the body alone isn't reliable on iOS Safari — the page behind a fixed
   * overlay can still rubber-band scroll via touchmove. Pinning the body at its current scroll
   * offset (the standard cross-browser trick) is what actually prevents that.
   */
  private lockBodyScroll(): void {
    this.scrollY = window.scrollY;
    document.body.style.position = 'fixed';
    document.body.style.top = `-${this.scrollY}px`;
    document.body.style.width = '100%';
  }

  private unlockBodyScroll(): void {
    document.body.style.position = '';
    document.body.style.top = '';
    document.body.style.width = '';
    window.scrollTo(0, this.scrollY);
  }
}
