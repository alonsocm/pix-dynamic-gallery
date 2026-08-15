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
 */
@Component({
  selector: 'app-wall',
  imports: [PhotoGridItemComponent, PhotoLightboxComponent],
  providers: [WallPhotosService], // route-scoped: a fresh instance (and fresh state) per navigation into this route
  template: `
    <img
      src="brand/pix-wordmark.png"
      alt="PIX"
      class="pointer-events-none fixed top-4 left-4 z-10 h-7 w-auto opacity-70 sm:h-9"
    />

    <div class="min-h-screen p-4 sm:p-6">
      <h1 class="mb-6 text-center text-2xl font-bold text-white/90">{{ event().name }} — Muro en vivo</h1>

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
    // prepended (allowSignalWrites — prependRealtime writes wallPhotos.photos internally).
    effect(
      () => {
        const notification = this.signalR.lastPhotoUploaded();
        if (notification && notification.eventId === this.event().id) {
          this.wallPhotos.prependRealtime(notification);
        }
      },
      { allowSignalWrites: true },
    );

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
