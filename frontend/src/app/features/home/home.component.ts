import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

/**
 * `/` — there's no single "home" experience in the product (guests always arrive via a scanned
 * QR pointing straight at `/e/:eventId/p/:photoId`, kiosks are pointed at `/kiosk/:eventId`
 * once during setup) — but the bare root still needs *something* other than falling through to
 * the generic not-found page, both for a sane first impression and as a quick way for a studio
 * operator to jump to an event without memorizing URLs.
 */
@Component({
  selector: 'app-home',
  imports: [RouterLink],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-8 px-6 text-center">
      <div>
        <h1 class="text-4xl font-bold text-brand-500">📸 Pix Dynamic Gallery</h1>
        <p class="mt-2 text-white/60">Plataforma en vivo para eventos con photobooth.</p>
      </div>

      <div class="flex w-full max-w-xs flex-col gap-3">
        <label class="text-left text-sm text-white/70" for="slug">Slug del evento</label>
        <input
          id="slug"
          type="text"
          placeholder="ej. julia-and-mark"
          class="rounded-full bg-white/10 px-4 py-2 text-white placeholder:text-white/30 outline-none focus:ring-2 focus:ring-brand-500"
          [value]="slug()"
          (input)="slug.set($any($event.target).value)"
          (keyup.enter)="goToWall()"
        />

        <div class="flex gap-3">
          <button
            type="button"
            (click)="goToKiosk()"
            [disabled]="!slug()"
            class="flex-1 rounded-full bg-white px-4 py-2 font-semibold text-ink-900 transition disabled:opacity-30"
          >
            🖥️ Kiosk
          </button>
          <button
            type="button"
            (click)="goToWall()"
            [disabled]="!slug()"
            class="flex-1 rounded-full bg-brand-500 px-4 py-2 font-semibold text-white transition disabled:opacity-30"
          >
            🧱 Muro
          </button>
        </div>
      </div>

      <p class="max-w-sm text-xs text-white/30">
        Los invitados llegan escaneando el QR que muestra el kiosk — esta página es solo para operadores.
      </p>

      <a routerLink="/admin/events/new" class="text-sm text-white/50 underline">
        ¿Sos el operador? Crear evento nuevo →
      </a>
    </div>
  `,
})
export class HomeComponent {
  private readonly router = inject(Router);

  protected readonly slug = signal('');

  goToKiosk(): void {
    if (this.slug()) {
      void this.router.navigate(['/kiosk', this.slug()]);
    }
  }

  goToWall(): void {
    if (this.slug()) {
      void this.router.navigate(['/e', this.slug(), 'wall']);
    }
  }
}
