import { Component, effect, input, signal } from '@angular/core';
import QRCode from 'qrcode';

/**
 * Renders `value()` as a QR code, generated entirely client-side (canvas → data URL, via the
 * `qrcode` npm package) — no external QR-generation API/network call.
 */
@Component({
  selector: 'app-qr-code',
  template: `
    @if (dataUrl(); as src) {
      <img [src]="src" [alt]="'Código QR'" class="aspect-square w-full rounded-photo bg-white p-3" />
    }
  `,
})
export class QrCodeComponent {
  readonly value = input.required<string>();

  /** Raster width of the generated PNG, in px. Bump this for anything meant to be printed
   * (the kiosk's default is plenty for an on-screen scan, but a QR printed a few inches wide
   * needs more source pixels to stay crisp instead of upscaling a blurry 512px source). */
  readonly size = input(512);

  protected readonly dataUrl = signal<string | null>(null);

  constructor() {
    effect(() => {
      const target = this.value();
      const width = this.size();
      QRCode.toDataURL(target, { width, margin: 1 })
        .then((url) => this.dataUrl.set(url))
        .catch(() => this.dataUrl.set(null));
    });
  }
}
