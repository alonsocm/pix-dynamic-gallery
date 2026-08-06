import { Component, DestroyRef, OnInit, computed, effect, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { PhotoFailedNotification, PhotoUploadedNotification } from '../../core/models/signalr-notifications.model';
import { SignalRService } from '../../core/signalr/signalr.service';
import { EventDto } from '../../core/models/event.model';
import { QrCodeComponent } from '../../shared/ui/qr-code/qr-code.component';

const FAILED_NOTICE_TTL_MS = 8000;

/**
 * `/kiosk/:eventId` — the physical photobooth's own screen. Shows the most recently uploaded
 * photo plus a QR code guests scan to open their own copy (`GuestPhotoComponent`). A one-time
 * REST fetch on init seeds the last known photo (so load/refresh never shows a blank state);
 * everything after that is realtime-driven via SignalR, no polling.
 */
@Component({
  selector: 'app-kiosk',
  imports: [QrCodeComponent],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-8 p-8">
      <h1 class="text-center text-3xl font-bold text-white/90">{{ event().name }}</h1>

      @if (failedNotices().length > 0) {
        <div class="flex w-full max-w-2xl flex-col gap-1 rounded-lg bg-red-600/90 p-3 text-sm text-white">
          @for (notice of failedNotices(); track notice.photoId) {
            <p>⚠️ No se pudo subir una foto: {{ notice.reason }}</p>
          }
        </div>
      }

      <div class="flex w-full max-w-4xl flex-col items-center gap-8 sm:flex-row sm:items-center sm:justify-center">
        <div class="flex flex-1 items-center justify-center">
          @if (latestPhoto(); as photo) {
            <img [src]="photo.url" alt="Última foto capturada" class="max-h-[60vh] rounded-photo shadow-2xl shadow-black/60" />
          } @else {
            <div
              class="flex aspect-[3/4] w-full max-w-sm flex-col items-center justify-center gap-3 rounded-photo border-2 border-dashed border-white/20 text-white/50"
            >
              <span class="text-6xl">📸</span>
              <p>Esperando la próxima foto…</p>
            </div>
          }
        </div>

        @if (qrTargetUrl(); as qrUrl) {
          <div class="flex w-full max-w-xs flex-col items-center gap-3">
            <app-qr-code [value]="qrUrl" />
            <p class="text-center text-sm text-white/70">Escanea para ver y compartir tu foto</p>
          </div>
        }
      </div>

      <p class="text-xs text-white/30">{{ connectionLabel() }}</p>
    </div>
  `,
})
export class KioskComponent implements OnInit {
  readonly event = input.required<EventDto>();

  private readonly signalR = inject(SignalRService);
  private readonly api = inject(ApiClient);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly failedNotices = signal<PhotoFailedNotification[]>([]);

  /**
   * Seeded once on init from the REST API (the already-uploaded photo history) so the kiosk shows
   * *something* immediately on load/refresh instead of a blank "waiting" state until the next live
   * upload — a real kiosk display gets refreshed/restarted sometimes, and going blank until the
   * next photo arrives would be a regression from "last known good state".
   */
  protected readonly initialLatestPhoto = signal<PhotoUploadedNotification | null>(null);

  /** Live SignalR pushes take priority once any arrive; the REST-seeded value is only the fallback. */
  protected readonly latestPhoto = computed(() => {
    const notification = this.signalR.lastPhotoUploaded();
    if (notification?.eventId === this.event().id) {
      return notification;
    }
    return this.initialLatestPhoto();
  });

  /** Mirrors Event.BuildGuestPhotoUrl on the backend exactly, now that EventDto exposes guestBaseUrl. */
  protected readonly qrTargetUrl = computed(() => {
    const photo = this.latestPhoto();
    if (!photo) {
      return null;
    }
    return `${this.event().guestBaseUrl}/e/${this.event().slug}/p/${photo.photoId}`;
  });

  protected readonly connectionLabel = computed(() => {
    switch (this.signalR.connectionState()) {
      case 'connected':
        return '● En vivo';
      case 'connecting':
        return '○ Conectando…';
      case 'reconnecting':
        return '○ Reconectando…';
      default:
        return '○ Desconectado';
    }
  });

  constructor() {
    // allowSignalWrites: this effect's whole job is pushing a new notice into failedNotices (and
    // later removing it) in reaction to signalR.lastPhotoFailed() changing — the standard,
    // Angular-sanctioned shape for "reactive signal in, side-effecting signal write out".
    effect(
      () => {
        const failure = this.signalR.lastPhotoFailed();
        if (!failure || failure.eventId !== this.event().id) {
          return;
        }

        this.failedNotices.update((list) => [...list, failure]);
        setTimeout(() => {
          this.failedNotices.update((list) => list.filter((n) => n !== failure));
        }, FAILED_NOTICE_TTL_MS);
      },
      { allowSignalWrites: true },
    );
  }

  // Required inputs aren't readable in the constructor (Angular binds them right after
  // construction, before ngOnInit) — join-on-init/leave-on-destroy lives here instead.
  ngOnInit(): void {
    const eventId = this.event().id;
    void this.signalR.joinEvent(eventId);
    this.destroyRef.onDestroy(() => void this.signalR.leaveEvent(eventId));
    void this.loadLatestPhoto(eventId);
  }

  /** GetEventPhotos is already newest-first, so page 1 / size 1 is exactly "the latest upload". */
  private async loadLatestPhoto(eventId: string): Promise<void> {
    const page = await firstValueFrom(this.api.getEventPhotos(eventId, 1, 1));
    const latest = page.items[0];
    if (!latest?.url) {
      return;
    }

    this.initialLatestPhoto.set({
      photoId: latest.id,
      eventId: latest.eventId,
      url: latest.url,
      timestamp: latest.uploadedAtUtc ?? latest.capturedAtUtc,
    });
  }
}
