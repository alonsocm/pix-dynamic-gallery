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

  protected readonly dataUrl = signal<string | null>(null);

  constructor() {
    effect(() => {
      const target = this.value();
      QRCode.toDataURL(target, { width: 512, margin: 1 })
        .then((url) => this.dataUrl.set(url))
        .catch(() => this.dataUrl.set(null));
    });
  }
}
