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
      <button
        type="button"
        (click)="download()"
        class="flex flex-1 items-center justify-center gap-2 rounded-full bg-white px-6 py-3 font-semibold text-ink-900 transition active:scale-95"
      >
        ⬇️ Descargar
      </button>

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

  /**
   * The photo lives on a different origin (Cloudflare R2) than the site itself, and the HTML
   * `download` attribute on a plain `<a href>` is only honored by browsers for same-origin URLs —
   * cross-origin, they just navigate to it instead (confirmed on a real phone: tapping "Descargar"
   * opened the photo in a new tab instead of saving it). Fetching the bytes and handing the browser
   * a same-origin `blob:` URL sidesteps that restriction. Requires the R2 bucket's CORS policy to
   * allow GET from this site's origin, or the fetch itself fails.
   *
   * Two more things confirmed necessary on a real iPhone (Safari): the link has to actually be
   * attached to the DOM for `.click()` to reliably trigger a download rather than a no-op, and
   * `URL.revokeObjectURL` can't happen right after `.click()` — Safari shows its own "Do you want
   * to download this file?" prompt first, and by the time the guest taps through it the blob was
   * already freed, so the download failed with a generic error. Delaying the revoke gives that
   * prompt time to be answered.
   */
  async download(): Promise<void> {
    try {
      const response = await fetch(this.url());
      const blob = await response.blob();
      const blobUrl = URL.createObjectURL(blob);

      const link = document.createElement('a');
      link.href = blobUrl;
      link.download = this.suggestedFilename();
      document.body.appendChild(link);
      link.click();
      link.remove();

      setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000);
    } catch {
      // CORS blocked, offline, etc. — fall back to just opening it so the guest can long-press
      // and save manually instead of the button silently doing nothing.
      window.open(this.url(), '_blank');
    }
  }

  private suggestedFilename(): string {
    const lastSegment = this.url().split('/').pop() || 'foto.jpg';
    return decodeURIComponent(lastSegment);
  }

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
