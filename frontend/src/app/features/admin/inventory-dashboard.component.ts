import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { localDateInputToIso } from '../../core/common/local-date';
import { PaperStockDto, UsbStockDto } from '../../core/models/inventory.model';

interface PaperPurchaseFormControls {
  purchaseDate: FormControl<string>;
  sheetsCount: FormControl<string>;
  totalCost: FormControl<string>;
  notes: FormControl<string>;
}

interface UsbPurchaseFormControls {
  purchaseDate: FormControl<string>;
  unitsCount: FormControl<string>;
  totalCost: FormControl<string>;
  notes: FormControl<string>;
}

function todayLocalDate(): string {
  return new Date().toISOString().slice(0, 10);
}

/** `/admin/inventory` — supply stock handed out or consumed at every event (paper/ink, USB drives). Kept separate from /admin/finance, which is money only. */
@Component({
  selector: 'app-inventory-dashboard',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6 flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold">Inventario</h1>
        <div class="flex flex-wrap gap-2">
          <a routerLink="/admin/events" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 🎪 Eventos </a>
          <a routerLink="/admin/agenda" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 📅 Agenda </a>
          <a routerLink="/admin/finance" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 💰 Finanzas </a>
        </div>
      </div>

      @if (loading()) {
        <p class="py-16 text-center text-white/50">Cargando…</p>
      } @else {
        <!-- Stock de papel -->
        <section class="mb-6 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">🧻 Stock de papel/tinta</h2>
          @if (paperStock(); as ps) {
            <div class="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-3">
              <p class="text-sm text-white/70">Hojas restantes: <strong class="text-white">{{ ps.remainingSheets }}</strong></p>
              <p class="text-sm text-white/70">Compradas: <strong class="text-white">{{ ps.totalPurchasedSheets }}</strong></p>
              <p class="text-sm text-white/70">Costo/foto sugerido: <strong class="text-white">{{ formatMoney(ps.suggestedCostPerPhoto) }}</strong></p>
            </div>

            @if (ps.purchases.length > 0) {
              <ul class="mt-3 flex flex-col gap-1 text-xs text-white/50">
                @for (p of ps.purchases; track p.id) {
                  <li class="flex items-center justify-between gap-2">
                    <span>{{ formatDate(p.purchaseDate) }} — {{ p.sheetsCount }} hojas por {{ formatMoney(p.totalCost) }} ({{ formatMoney(p.costPerSheet) }}/hoja) @if (p.notes) { · {{ p.notes }} }</span>
                    <button type="button" (click)="removePaperPurchase(p.id)" aria-label="Borrar compra de papel" class="shrink-0 text-white/40 hover:text-red-300">🗑️</button>
                  </li>
                }
              </ul>
            }
          }

          <form [formGroup]="paperForm" (ngSubmit)="submitPaperPurchase()" class="mt-3 flex flex-wrap items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Fecha</span>
              <input type="date" formControlName="purchaseDate" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Hojas (kit RP-108 = 108)</span>
              <input type="number" min="1" formControlName="sheetsCount" class="w-28 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Costo total</span>
              <input type="number" min="0" step="0.01" formControlName="totalCost" class="w-28 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-1 flex-col gap-1">
              <span class="text-xs text-white/50">Notas</span>
              <input formControlName="notes" placeholder="Mercado Libre, promoción, etc." class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <button type="submit" [disabled]="paperForm.invalid || savingPaper()" class="rounded-full bg-brand-600 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              + Compra
            </button>
          </form>
        </section>

        <!-- Stock de USBs -->
        <section class="rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">🔌 Stock de USBs</h2>
          @if (usbStock(); as us) {
            <div class="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-3">
              <p class="text-sm text-white/70">Unidades restantes: <strong class="text-white">{{ us.remainingUnits }}</strong></p>
              <p class="text-sm text-white/70">Compradas: <strong class="text-white">{{ us.totalPurchasedUnits }}</strong></p>
              <p class="text-sm text-white/70">Costo/USB sugerido: <strong class="text-white">{{ formatMoney(us.suggestedCostPerUsb) }}</strong></p>
            </div>

            @if (us.purchases.length > 0) {
              <ul class="mt-3 flex flex-col gap-1 text-xs text-white/50">
                @for (p of us.purchases; track p.id) {
                  <li class="flex items-center justify-between gap-2">
                    <span>{{ formatDate(p.purchaseDate) }} — {{ p.unitsCount }} USB por {{ formatMoney(p.totalCost) }} ({{ formatMoney(p.costPerUnit) }}/u) @if (p.notes) { · {{ p.notes }} }</span>
                    <button type="button" (click)="removeUsbPurchase(p.id)" aria-label="Borrar compra de USB" class="shrink-0 text-white/40 hover:text-red-300">🗑️</button>
                  </li>
                }
              </ul>
            }
          }

          <form [formGroup]="usbForm" (ngSubmit)="submitUsbPurchase()" class="mt-3 flex flex-wrap items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Fecha</span>
              <input type="date" formControlName="purchaseDate" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Unidades</span>
              <input type="number" min="1" formControlName="unitsCount" class="w-24 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Costo total</span>
              <input type="number" min="0" step="0.01" formControlName="totalCost" class="w-28 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-1 flex-col gap-1">
              <span class="text-xs text-white/50">Notas</span>
              <input formControlName="notes" placeholder="Mercado Libre, proveedor, etc." class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <button type="submit" [disabled]="usbForm.invalid || savingUsb()" class="rounded-full bg-brand-600 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              + Compra
            </button>
          </form>
        </section>
      }
    </div>
  `,
})
export class InventoryDashboardComponent implements OnInit {
  private readonly api = inject(ApiClient);

  protected readonly loading = signal(true);
  protected readonly paperStock = signal<PaperStockDto | null>(null);
  protected readonly usbStock = signal<UsbStockDto | null>(null);
  protected readonly savingPaper = signal(false);
  protected readonly savingUsb = signal(false);

  protected readonly paperForm = new FormGroup<PaperPurchaseFormControls>({
    purchaseDate: new FormControl(todayLocalDate(), { nonNullable: true, validators: [Validators.required] }),
    sheetsCount: new FormControl('108', { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    totalCost: new FormControl('800', { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    notes: new FormControl('', { nonNullable: true }),
  });

  protected readonly usbForm = new FormGroup<UsbPurchaseFormControls>({
    purchaseDate: new FormControl(todayLocalDate(), { nonNullable: true, validators: [Validators.required] }),
    unitsCount: new FormControl('1', { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    totalCost: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    notes: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void {
    this.reload();
  }

  protected formatMoney(value: number): string {
    return value.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' });
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('es-MX', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  protected submitPaperPurchase(): void {
    if (this.paperForm.invalid) {
      return;
    }
    const raw = this.paperForm.getRawValue();
    this.savingPaper.set(true);
    this.api
      .addPaperPurchase({
        purchaseDate: localDateInputToIso(raw.purchaseDate),
        sheetsCount: Number(raw.sheetsCount),
        totalCost: Number(raw.totalCost),
        notes: raw.notes || null,
      })
      .subscribe({
        next: () => {
          this.savingPaper.set(false);
          this.paperForm.patchValue({ notes: '' });
          this.reloadPaperStock();
        },
        error: () => this.savingPaper.set(false),
      });
  }

  protected removePaperPurchase(id: string): void {
    if (!window.confirm('¿Borrar esta compra de papel?')) {
      return;
    }
    this.api.deletePaperPurchase(id).subscribe({
      next: () => this.reloadPaperStock(),
    });
  }

  protected submitUsbPurchase(): void {
    if (this.usbForm.invalid) {
      return;
    }
    const raw = this.usbForm.getRawValue();
    this.savingUsb.set(true);
    this.api
      .addUsbPurchase({
        purchaseDate: localDateInputToIso(raw.purchaseDate),
        unitsCount: Number(raw.unitsCount),
        totalCost: Number(raw.totalCost),
        notes: raw.notes || null,
      })
      .subscribe({
        next: () => {
          this.savingUsb.set(false);
          this.usbForm.patchValue({ notes: '' });
          this.reloadUsbStock();
        },
        error: () => this.savingUsb.set(false),
      });
  }

  protected removeUsbPurchase(id: string): void {
    if (!window.confirm('¿Borrar esta compra de USBs?')) {
      return;
    }
    this.api.deleteUsbPurchase(id).subscribe({
      next: () => this.reloadUsbStock(),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.getPaperStock().subscribe({
      next: (stock) => {
        this.paperStock.set(stock);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.api.getUsbStock().subscribe((stock) => this.usbStock.set(stock));
  }

  private reloadPaperStock(): void {
    this.api.getPaperStock().subscribe((stock) => this.paperStock.set(stock));
  }

  private reloadUsbStock(): void {
    this.api.getUsbStock().subscribe((stock) => this.usbStock.set(stock));
  }
}
