import { Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { buildGuestWallUrl } from '../../core/event/guest-photo-url';
import { EventDto } from '../../core/models/event.model';
import { renderQrPrintCardImage } from './qr-print-card-image';
import { QrCodeComponent } from '../../shared/ui/qr-code/qr-code.component';

/**
 * `/admin/events/:eventId/qr` — a print-ready placard for one event's Wall, meant to come out of
 * the same Canon printer as the guest photos and go straight into a picture frame at the venue.
 * `:eventId` is the slug, resolved via eventResolver like event-photos/event-finance.
 *
 * The card itself is the one element on screen that's designed to leave the system as a physical
 * object (DESIGN.md's "Paper Exception Rule") — everything else on this page is the usual
 * void-black admin chrome, hidden via `print:hidden` so only the card comes out of the printer.
 *
 * Its palette is a deliberate, scoped exception to this app's own one-magenta-accent system: the
 * card is marketing collateral (it sits framed at the venue, next to the actual photobooth), so it
 * borrows the real brand gradient from the somospix.com landing page — pink `#FF007F` → purple
 * `#9333EA` → cyan `#00E5FF`, plus a gold confetti accent — instead of blending into the admin UI
 * around it. Nothing outside `.qr-print-card` uses these colors.
 */
@Component({
  selector: 'app-event-qr',
  imports: [RouterLink, QrCodeComponent],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6 print:hidden">
        <a routerLink="/admin/events" class="text-sm text-white/50">← Eventos</a>
        <h1 class="text-2xl font-bold text-white/90">{{ event().name }} — QR para imprimir</h1>
        <p class="mt-1 text-sm text-white/50">
          Imprímelo en la Canon a 10&times;15&nbsp;cm (4&times;6&nbsp;in) y ponlo en un
          portarretratos junto al photobooth — cada escaneo abre el muro en vivo del evento.
        </p>
      </div>

      <div class="flex flex-col items-center gap-6 print:block">
        <!-- Gradient "mat" frame around the printable card, like a colored picture-frame mat —
             the outer shape stays square-ish (rounded-3xl, not a pill) since real photo paper gets
             cut straight; the color band is what reads as "party," not the silhouette. -->
        <div
          class="qr-print-card relative aspect-[2/3] w-full max-w-sm rounded-3xl bg-linear-to-br from-[#FF007F] via-[#9333EA] to-[#00E5FF] p-2.5 shadow-2xl shadow-black/50 print:aspect-auto print:h-screen print:w-screen print:rounded-none print:p-[3%] print:shadow-none"
        >
          <div
            class="relative flex h-full w-full flex-col items-center justify-between gap-6 rounded-2xl bg-white px-8 py-10 text-center"
          >
            <!-- Confetti accents — purely decorative, tiny, printed along with everything else. -->
            <span class="absolute top-6 left-7 h-2.5 w-2.5 rounded-full bg-[#FFD700]"></span>
            <span class="absolute top-14 right-9 h-2 w-2 rounded-full bg-[#00E5FF]"></span>
            <span class="absolute bottom-24 left-9 h-2 w-2 rounded-full bg-[#9333EA]"></span>
            <span class="absolute right-7 bottom-10 h-2.5 w-2.5 rounded-full bg-[#FF007F]"></span>

            <span
              class="bg-linear-to-r from-[#FF007F] via-[#9333EA] to-[#00E5FF] bg-clip-text text-3xl font-black tracking-[0.15em] text-transparent"
            >
              PIX
            </span>

            <div class="flex w-full flex-col items-center gap-4">
              <h2 class="text-2xl leading-tight font-bold text-ink-900">{{ event().name }}</h2>

              <div class="rounded-2xl bg-linear-to-br from-[#FF007F] via-[#9333EA] to-[#00E5FF] p-[5px]">
                <div class="w-[210px] rounded-xl bg-white p-2">
                  <app-qr-code [value]="wallUrl()" [size]="1024" />
                </div>
              </div>

              <p class="text-sm font-semibold text-black/60">
                <span class="bg-linear-to-r from-[#FF007F] to-[#9333EA] bg-clip-text text-transparent">Escanea</span>
                para ver todas las fotos 📸
              </p>
            </div>

            <p class="font-mono text-[10px] break-all text-black/35">{{ wallUrl() }}</p>
          </div>
        </div>

        <div class="flex flex-col items-center gap-3 print:hidden">
          <div class="flex flex-wrap justify-center gap-2">
            <button
              type="button"
              (click)="downloadImage()"
              [disabled]="downloading()"
              class="rounded-full bg-linear-to-r from-[#FF007F] via-[#9333EA] to-[#FF007F] px-6 py-3 text-sm font-semibold text-white shadow-[0_0_20px_rgba(255,0,127,0.45),0_0_40px_rgba(255,0,127,0.2)] active:scale-95 disabled:opacity-50"
            >
              {{ downloading() ? 'Generando…' : '⬇️ Descargar imagen' }}
            </button>
            <button
              type="button"
              (click)="copyLink()"
              class="rounded-full bg-white/10 px-6 py-3 text-sm font-semibold text-white active:scale-95"
            >
              {{ copied() ? '✅ ¡Copiado!' : '🔗 Copiar link' }}
            </button>
            <button
              type="button"
              (click)="print()"
              class="rounded-full bg-white/10 px-6 py-3 text-sm font-semibold text-white/70 active:scale-95"
            >
              🖨️ Imprimir desde el navegador
            </button>
          </div>
          <p class="max-w-sm text-center text-xs text-white/40">
            En una impresora fotográfica dedicada (Canon SELPHY y similares) usa
            <strong class="text-white/60">Descargar imagen</strong> e imprímela desde la app de
            Canon o por USB/SD — el diálogo de impresión del navegador puede deformar el diseño o
            repartirlo en dos hojas en ese tipo de impresoras.
          </p>
          @if (downloadError()) {
            <p class="text-xs text-red-400">{{ downloadError() }}</p>
          }
        </div>
      </div>
    </div>
  `,
})
export class EventQrComponent {
  readonly event = input.required<EventDto>();

  protected readonly copied = signal(false);
  protected readonly downloading = signal(false);
  protected readonly downloadError = signal<string | null>(null);
  protected readonly wallUrl = computed(() => buildGuestWallUrl(this.event()));

  protected print(): void {
    window.print();
  }

  protected copyLink(): void {
    void navigator.clipboard.writeText(this.wallUrl()).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  /** Renders the card as a flat JPEG (see qr-print-card-image.ts for why) and downloads it. */
  protected async downloadImage(): Promise<void> {
    this.downloading.set(true);
    this.downloadError.set(null);

    try {
      const blob = await renderQrPrintCardImage({ eventName: this.event().name, wallUrl: this.wallUrl() });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `qr-muro-${this.event().slug}.jpg`;
      link.click();
      URL.revokeObjectURL(url);
    } catch {
      this.downloadError.set('No se pudo generar la imagen. Intenta de nuevo.');
    } finally {
      this.downloading.set(false);
    }
  }
}
