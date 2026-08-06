import { Component, input, signal } from '@angular/core';

/**
 * Download button plus a Share button that feature-detects the Web Share API — most desktop
 * browsers don't support `navigator.share`, so those get a "copy link" fallback instead so the
 * guest still has a way to distribute the photo.
 */
@Component({
  selector: 'app-download-share-buttons',
  template: `
    <div class="flex w-full gap-3">
      <a
        [href]="url()"
        download
        class="flex flex-1 items-center justify-center gap-2 rounded-full bg-white px-6 py-3 font-semibold text-ink-900 transition active:scale-95"
      >
        ⬇️ Descargar
      </a>

      @if (canShare()) {
        <button
          type="button"
          (click)="share()"
          class="flex flex-1 items-center justify-center gap-2 rounded-full bg-brand-500 px-6 py-3 font-semibold text-white transition active:scale-95"
        >
          📤 Compartir
        </button>
      } @else {
        <button
          type="button"
          (click)="copyLink()"
          class="flex flex-1 items-center justify-center gap-2 rounded-full bg-brand-500 px-6 py-3 font-semibold text-white transition active:scale-95"
        >
          {{ copied() ? '✅ ¡Copiado!' : '🔗 Copiar link' }}
        </button>
      }
    </div>
  `,
})
export class DownloadShareButtonsComponent {
  readonly url = input.required<string>();
  readonly title = input<string>('');

  protected readonly canShare = signal(typeof navigator !== 'undefined' && !!navigator.share);
  protected readonly copied = signal(false);

  async share(): Promise<void> {
    try {
      await navigator.share({ url: this.url(), title: this.title() || 'Mi foto' });
    } catch {
      // User cancelled the native share sheet — not an error worth surfacing.
    }
  }

  async copyLink(): Promise<void> {
    await navigator.clipboard.writeText(this.url());
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 2000);
  }
}
