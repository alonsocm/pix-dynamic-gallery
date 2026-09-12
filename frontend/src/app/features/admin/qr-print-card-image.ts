import QRCode from 'qrcode';

/**
 * Renders EventQrComponent's placard as a flat JPEG at true 4x6in print resolution (1200x1800px
 * @300dpi) instead of relying on the browser's print dialog.
 *
 * Why: a real Canon SELPHY warped this card's layout and split it across two sheets when printed
 * via Chrome's print dialog — dedicated dye-sub photo printers are fed through their own
 * app/USB/SD workflow, and their driver's fixed paper size ("Postcard", ~100x148mm) doesn't line
 * up with a generic web page's CSS `@page`. A plain JPEG sidesteps the browser print pipeline
 * entirely; it's also exactly how every guest photo already reaches the Canon in this project — a
 * file, not a browser print job.
 *
 * This is a from-scratch canvas re-render of the on-screen card, not a screenshot of it, so the
 * layout numbers below are tuned by eye against that design rather than derived from its DOM.
 */

const WIDTH = 1200;
const HEIGHT = 1800;

// Must match the `from-[...]`/`via-[...]`/`to-[...]` arbitrary Tailwind colors in
// EventQrComponent's template — kept in sync by convention, not by sharing a single source.
const PINK = '#FF007F';
const PURPLE = '#9333EA';
const CYAN = '#00E5FF';
const GOLD = '#FFD700';
const INK = '#18181b';

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error('No se pudo generar el QR.'));
    img.src = src;
  });
}

/** Canvas has no cross-browser roundRect guarantee, so path it by hand. */
function roundRectPath(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number): void {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
}

/** Shrinks the font size (via `makeFont`) until `text` fits `maxWidth`, down to `minSize`. Leaves `ctx.font` set to the chosen size. */
function fitFontSize(
  ctx: CanvasRenderingContext2D,
  text: string,
  makeFont: (size: number) => string,
  maxWidth: number,
  startSize: number,
  minSize = 24,
): void {
  let size = startSize;
  while (size > minSize) {
    ctx.font = makeFont(size);
    if (ctx.measureText(text).width <= maxWidth) {
      return;
    }
    size -= 2;
  }
  ctx.font = makeFont(size);
}

function dot(ctx: CanvasRenderingContext2D, x: number, y: number, r: number, color: string): void {
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fillStyle = color;
  ctx.fill();
}

export interface QrPrintCardOptions {
  eventName: string;
  wallUrl: string;
}

export async function renderQrPrintCardImage({ eventName, wallUrl }: QrPrintCardOptions): Promise<Blob> {
  const qrDataUrl = await QRCode.toDataURL(wallUrl, { width: 900, margin: 1 });
  const qrImage = await loadImage(qrDataUrl);

  const canvas = document.createElement('canvas');
  canvas.width = WIDTH;
  canvas.height = HEIGHT;
  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Canvas 2D no disponible.');
  }

  const brandGradient = (x0: number, y0: number, x1: number, y1: number): CanvasGradient => {
    const gradient = ctx.createLinearGradient(x0, y0, x1, y1);
    gradient.addColorStop(0, PINK);
    gradient.addColorStop(0.5, PURPLE);
    gradient.addColorStop(1, CYAN);
    return gradient;
  };

  // Outer gradient "mat" frame.
  roundRectPath(ctx, 0, 0, WIDTH, HEIGHT, 90);
  ctx.fillStyle = brandGradient(0, 0, WIDTH, HEIGHT);
  ctx.fill();

  // Inner white card.
  const frame = 40;
  const cardX = frame;
  const cardY = frame;
  const cardW = WIDTH - frame * 2;
  const cardH = HEIGHT - frame * 2;
  roundRectPath(ctx, cardX, cardY, cardW, cardH, 60);
  ctx.fillStyle = '#ffffff';
  ctx.fill();

  // Confetti accents, near the four corners.
  dot(ctx, cardX + 100, cardY + 110, 14, GOLD);
  dot(ctx, cardX + cardW - 100, cardY + 170, 11, CYAN);
  dot(ctx, cardX + 110, cardY + cardH - 250, 11, PURPLE);
  dot(ctx, cardX + cardW - 110, cardY + cardH - 180, 14, PINK);

  const centerX = WIDTH / 2;
  const contentWidth = cardW - 180; // 90px inset each side, matching the on-screen card's padding

  ctx.textAlign = 'center';
  ctx.textBaseline = 'alphabetic';

  // "PIX" wordmark.
  const wordmarkY = cardY + 265;
  ctx.font = '800 76px Arial, sans-serif';
  ctx.fillStyle = brandGradient(centerX - 120, wordmarkY, centerX + 120, wordmarkY);
  ctx.fillText('P I X', centerX, wordmarkY);

  // Event name — shrinks to fit instead of overflowing/wrapping.
  const nameY = cardY + 543;
  fitFontSize(ctx, eventName, (s) => `800 ${s}px Arial, sans-serif`, contentWidth, 64);
  ctx.fillStyle = INK;
  ctx.fillText(eventName, centerX, nameY);

  // QR, framed the same way as the outer card (gradient ring + white inset).
  const qrBoxSize = 640;
  const qrBoxX = centerX - qrBoxSize / 2;
  const qrBoxY = cardY + 647;
  roundRectPath(ctx, qrBoxX, qrBoxY, qrBoxSize, qrBoxSize, 44);
  ctx.fillStyle = brandGradient(qrBoxX, qrBoxY, qrBoxX + qrBoxSize, qrBoxY + qrBoxSize);
  ctx.fill();
  const qrInset = 20;
  roundRectPath(ctx, qrBoxX + qrInset, qrBoxY + qrInset, qrBoxSize - qrInset * 2, qrBoxSize - qrInset * 2, 30);
  ctx.fillStyle = '#ffffff';
  ctx.fill();
  const qrDrawSize = qrBoxSize - qrInset * 2 - 60;
  ctx.drawImage(qrImage, qrBoxX + qrInset + 30, qrBoxY + qrInset + 30, qrDrawSize, qrDrawSize);

  // Caption: "Escanea" in the brand gradient, the rest muted dark.
  const captionY = qrBoxY + qrBoxSize + 80;
  ctx.font = '700 34px Arial, sans-serif';
  const word1 = 'Escanea';
  const word2 = ' para ver todas las fotos \u{1F4F8}';
  const w1 = ctx.measureText(word1).width;
  const w2 = ctx.measureText(word2).width;
  let cursorX = centerX - (w1 + w2) / 2;
  ctx.textAlign = 'left';
  ctx.fillStyle = brandGradient(cursorX, captionY, cursorX + w1, captionY);
  ctx.fillText(word1, cursorX, captionY);
  cursorX += w1;
  ctx.fillStyle = 'rgba(0,0,0,0.6)';
  ctx.fillText(word2, cursorX, captionY);

  // Wall URL, small and muted, as a fallback for anyone who can't scan.
  ctx.textAlign = 'center';
  fitFontSize(ctx, wallUrl, (s) => `${s}px "Courier New", monospace`, contentWidth, 26, 16);
  ctx.fillStyle = 'rgba(0,0,0,0.35)';
  ctx.fillText(wallUrl, centerX, cardY + cardH - 106);

  return new Promise<Blob>((resolve, reject) => {
    canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('No se pudo generar la imagen.'))), 'image/jpeg', 0.95);
  });
}
