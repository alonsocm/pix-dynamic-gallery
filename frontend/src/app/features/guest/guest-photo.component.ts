import { Component, OnInit, inject, input, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { EventDto } from '../../core/models/event.model';
import { PhotoDto, PhotoStatus } from '../../core/models/photo.model';
import { DownloadShareButtonsComponent } from '../../shared/ui/download-share-buttons/download-share-buttons.component';

const RETRY_ATTEMPTS = 5;
const RETRY_DELAY_MS = 2000;

/**
 * Guest landing page (`/e/:eventId/p/:photoId`), the page a scanned QR opens. `event`/`photo`
 * arrive pre-resolved from the route (see event.resolver.ts/photo.resolver.ts) — no loading
 * spinner for the common case, since by the time a QR is scannable at all, the kiosk has already
 * received `OnPhotoUploaded`, which itself only fires after the photo's DB status flips to
 * Uploaded. The retry-poll below is cheap insurance against pathological edge cases (a stale QR
 * reused after later failure, a race on an extremely fast scan), not the designed happy path.
 */
@Component({
  selector: 'app-guest-photo',
  imports: [DownloadShareButtonsComponent],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-6 px-4 py-8">
      @if (currentPhoto(); as p) {
        @switch (p.status) {
          @case (PhotoStatus.Uploaded) {
            <div class="flex w-full max-w-md flex-col items-center gap-6">
              <img [src]="p.url" [alt]="event().name" class="w-full rounded-photo shadow-2xl shadow-black/50" />
              <h1 class="text-center text-lg font-semibold text-white/90">{{ event().name }}</h1>
              <app-download-share-buttons [url]="p.url!" [title]="event().name" />
            </div>
          }
          @case (PhotoStatus.Failed) {
            <div class="flex flex-col items-center gap-2 text-center">
              <span class="text-5xl">😕</span>
              <p class="text-white/80">No pudimos procesar esta foto. Pídele al fotógrafo que la vuelva a tomar.</p>
            </div>
          }
          @default {
            <div class="flex flex-col items-center gap-3 text-center">
              <span class="animate-pulse text-5xl">📸</span>
              <p class="text-white/80">Tu foto se está procesando…</p>
            </div>
          }
        }
      }
    </div>
  `,
})
export class GuestPhotoComponent implements OnInit {
  readonly event = input.required<EventDto>();
  readonly photo = input.required<PhotoDto>();

  private readonly api = inject(ApiClient);

  protected readonly PhotoStatus = PhotoStatus;
  protected readonly currentPhoto = signal<PhotoDto | null>(null);

  ngOnInit(): void {
    const initial = this.photo();
    this.currentPhoto.set(initial);

    if (initial.status !== PhotoStatus.Uploaded) {
      void this.pollUntilUploaded();
    }
  }

  private async pollUntilUploaded(): Promise<void> {
    for (let attempt = 0; attempt < RETRY_ATTEMPTS; attempt++) {
      await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));

      const updated = await firstValueFrom(this.api.getPhoto(this.event().id, this.photo().id));
      this.currentPhoto.set(updated);

      if (updated.status === PhotoStatus.Uploaded || updated.status === PhotoStatus.Failed) {
        return;
      }
    }
  }
}
