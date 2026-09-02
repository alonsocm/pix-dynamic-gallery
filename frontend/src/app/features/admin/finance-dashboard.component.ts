import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { EXPENSE_CATEGORIES, FINANCE_CATEGORY_LABELS, FinanceCategory, FinanceDashboardDto } from '../../core/models/finance.model';

interface GlobalExpenseFormControls {
  expenseDate: FormControl<string>;
  category: FormControl<FinanceCategory>;
  description: FormControl<string>;
  amount: FormControl<string>;
}

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

/** `/admin/finance` — studio-wide profit/loss dashboard: totals, per-event breakdown, paper stock, global expenses, settings. */
@Component({
  selector: 'app-finance-dashboard',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6 flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold">Finanzas</h1>
        <div class="flex gap-2">
          <a routerLink="/admin/events" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 🎪 Eventos </a>
          <a routerLink="/admin/agenda" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 📅 Agenda </a>
        </div>
      </div>

      @if (loading()) {
        <p class="py-16 text-center text-white/50">Cargando…</p>
      } @else if (dashboard(); as d) {
        <!-- Totales globales -->
        <div class="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div class="rounded-lg bg-white/10 p-4">
            <p class="text-xs text-white/50">Ingresos totales</p>
            <p class="mt-1 text-xl font-bold text-emerald-400">{{ formatMoney(d.totalIncome) }}</p>
            @if (d.pendingDepositsTotal > 0) {
              <p class="mt-1 text-[11px] text-white/40">incluye {{ formatMoney(d.pendingDepositsTotal) }} en anticipos de agenda</p>
            }
          </div>
          <div class="rounded-lg bg-white/10 p-4">
            <p class="text-xs text-white/50">Gastos reales</p>
            <p class="mt-1 text-xl font-bold text-red-400">{{ formatMoney(d.totalRealExpenses) }}</p>
          </div>
          <div class="rounded-lg bg-white/10 p-4">
            <p class="text-xs text-white/50">Ganancia neta</p>
            <p class="mt-1 text-xl font-bold" [class.text-emerald-400]="d.netProfit >= 0" [class.text-red-400]="d.netProfit < 0">
              {{ formatMoney(d.netProfit) }}
            </p>
          </div>
        </div>

        <!-- Anticipos de agenda pendientes de evento -->
        @if (d.agendaDeposits.length > 0) {
          <section class="mb-6 rounded-lg bg-white/10 p-4">
            <h2 class="font-semibold">📅 Anticipos de agenda pendientes de evento</h2>
            <p class="mt-1 text-xs text-white/40">
              Ya cuentan como ingreso arriba — cuando crees el evento técnico y lo vincules, pasan a ser el ingreso de ese evento.
            </p>
            <ul class="mt-2 flex flex-col gap-1 text-sm">
              @for (dep of d.agendaDeposits; track dep.agendaEntryId) {
                <li class="flex items-center justify-between gap-2">
                  <a routerLink="/admin/agenda" class="truncate text-white/80 hover:underline">
                    {{ dep.clientName }} <span class="text-white/40">· {{ formatDate(dep.eventDate) }}</span>
                  </a>
                  <span class="font-semibold text-emerald-400">{{ formatMoney(dep.total) }}</span>
                </li>
              }
            </ul>
          </section>
        }

        <!-- Stock de papel -->
        <section class="mb-6 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">🧻 Stock de papel/tinta</h2>
          <div class="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-3">
            <p class="text-sm text-white/70">Hojas restantes: <strong class="text-white">{{ d.paperStock.remainingSheets }}</strong></p>
            <p class="text-sm text-white/70">Compradas: <strong class="text-white">{{ d.paperStock.totalPurchasedSheets }}</strong></p>
            <p class="text-sm text-white/70">Costo/foto sugerido: <strong class="text-white">{{ formatMoney(d.paperStock.suggestedCostPerPhoto) }}</strong></p>
          </div>

          @if (d.paperStock.purchases.length > 0) {
            <ul class="mt-3 flex flex-col gap-1 text-xs text-white/50">
              @for (p of d.paperStock.purchases; track p.id) {
                <li class="flex items-center justify-between gap-2">
                  <span>{{ formatDate(p.purchaseDate) }} — {{ p.sheetsCount }} hojas por {{ formatMoney(p.totalCost) }} ({{ formatMoney(p.costPerSheet) }}/hoja) @if (p.notes) { · {{ p.notes }} }</span>
                  <button type="button" (click)="removePaperPurchase(p.id)" class="shrink-0 text-white/40 hover:text-red-300">🗑️</button>
                </li>
              }
            </ul>
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
            <button type="submit" [disabled]="paperForm.invalid || savingPaper()" class="rounded-full bg-brand-500 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              + Compra
            </button>
          </form>
        </section>

        <!-- Stock de USBs -->
        <section class="mb-6 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">🔌 Stock de USBs</h2>
          <div class="mt-2 grid grid-cols-1 gap-3 sm:grid-cols-3">
            <p class="text-sm text-white/70">Unidades restantes: <strong class="text-white">{{ d.usbStock.remainingUnits }}</strong></p>
            <p class="text-sm text-white/70">Compradas: <strong class="text-white">{{ d.usbStock.totalPurchasedUnits }}</strong></p>
            <p class="text-sm text-white/70">Costo/USB sugerido: <strong class="text-white">{{ formatMoney(d.usbStock.suggestedCostPerUsb) }}</strong></p>
          </div>

          @if (d.usbStock.purchases.length > 0) {
            <ul class="mt-3 flex flex-col gap-1 text-xs text-white/50">
              @for (p of d.usbStock.purchases; track p.id) {
                <li class="flex items-center justify-between gap-2">
                  <span>{{ formatDate(p.purchaseDate) }} — {{ p.unitsCount }} USB por {{ formatMoney(p.totalCost) }} ({{ formatMoney(p.costPerUnit) }}/u) @if (p.notes) { · {{ p.notes }} }</span>
                  <button type="button" (click)="removeUsbPurchase(p.id)" class="shrink-0 text-white/40 hover:text-red-300">🗑️</button>
                </li>
              }
            </ul>
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
            <button type="submit" [disabled]="usbForm.invalid || savingUsb()" class="rounded-full bg-brand-500 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              + Compra
            </button>
          </form>
        </section>

        <!-- Costo por km -->
        <section class="mb-6 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">⛽ Costo por km</h2>
          <div class="mt-2 flex items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Costo por km</span>
              <input
                type="number"
                min="0"
                step="0.01"
                [value]="costPerKmInput()"
                (input)="costPerKmInput.set($any($event.target).value)"
                class="w-28 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500"
              />
            </label>
            <button type="button" (click)="saveCostPerKm()" class="rounded-full bg-white/10 px-4 py-1.5 text-sm font-semibold text-white">
              Guardar
            </button>
          </div>
        </section>

        <!-- Gastos globales -->
        <section class="mb-6 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">🧾 Gastos globales</h2>
          <p class="mt-1 text-xs text-white/40">No atados a un evento específico (equipo, marketing, etc.).</p>

          @if (globalExpenses().length > 0) {
            <ul class="mt-3 flex flex-col gap-1 text-sm">
              @for (e of globalExpenses(); track e.id) {
                <li class="flex items-center justify-between gap-2 text-white/70">
                  <span>{{ formatDate(e.expenseDate) }} · {{ categoryLabel(e.category) }} · {{ e.description }}</span>
                  <span class="flex items-center gap-2">
                    <span class="text-red-400">{{ formatMoney(e.amount) }}</span>
                    <button type="button" (click)="removeGlobalExpense(e.id)" class="text-xs text-white/40 hover:text-red-300">🗑️</button>
                  </span>
                </li>
              }
            </ul>
          }

          <form [formGroup]="expenseForm" (ngSubmit)="submitGlobalExpense()" class="mt-3 flex flex-wrap items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Fecha</span>
              <input type="date" formControlName="expenseDate" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Categoría</span>
              <select formControlName="category" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500">
                @for (c of expenseCategories; track c) {
                  <option [ngValue]="c">{{ categoryLabel(c) }}</option>
                }
              </select>
            </label>
            <label class="flex flex-1 flex-col gap-1">
              <span class="text-xs text-white/50">Descripción</span>
              <input formControlName="description" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Monto</span>
              <input type="number" min="0" step="0.01" formControlName="amount" class="w-28 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <button type="submit" [disabled]="expenseForm.invalid || savingExpense()" class="rounded-full bg-brand-500 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              + Gasto
            </button>
          </form>
        </section>

        <!-- Desglose por evento -->
        <section class="rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">📊 Ganancia por evento</h2>
          @if (d.perEventBreakdown.length === 0) {
            <p class="mt-2 text-sm text-white/40">Todavía no hay movimientos registrados en ningún evento.</p>
          } @else {
            <ul class="mt-2 flex flex-col gap-1 text-sm">
              @for (b of d.perEventBreakdown; track b.eventId) {
                <li class="flex items-center justify-between gap-2">
                  <a [routerLink]="['/admin/events', b.eventId, 'finance']" class="truncate text-white/80 hover:underline">{{ b.eventName }}</a>
                  <span [class.text-emerald-400]="b.profit >= 0" [class.text-red-400]="b.profit < 0" class="font-semibold">
                    {{ formatMoney(b.profit) }}
                  </span>
                </li>
              }
            </ul>
          }
        </section>
      }
    </div>
  `,
})
export class FinanceDashboardComponent implements OnInit {
  private readonly api = inject(ApiClient);

  protected readonly expenseCategories = EXPENSE_CATEGORIES;
  protected readonly loading = signal(true);
  protected readonly dashboard = signal<FinanceDashboardDto | null>(null);
  protected readonly globalExpenses = signal<{ id: string; expenseDate: string; category: FinanceCategory; description: string; amount: number }[]>([]);
  protected readonly costPerKmInput = signal('0');
  protected readonly savingPaper = signal(false);
  protected readonly savingUsb = signal(false);
  protected readonly savingExpense = signal(false);

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

  protected readonly expenseForm = new FormGroup<GlobalExpenseFormControls>({
    expenseDate: new FormControl(todayLocalDate(), { nonNullable: true, validators: [Validators.required] }),
    category: new FormControl<FinanceCategory>(FinanceCategory.Equipment, { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    amount: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] }),
  });

  ngOnInit(): void {
    this.reload();
    this.api.getFinanceSettings().subscribe((s) => this.costPerKmInput.set(String(s.costPerKm)));
    this.api.getGlobalExpenses().subscribe((expenses) => this.globalExpenses.set(expenses));
  }

  protected categoryLabel(category: FinanceCategory): string {
    return FINANCE_CATEGORY_LABELS[category];
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
        purchaseDate: new Date(raw.purchaseDate).toISOString(),
        sheetsCount: Number(raw.sheetsCount),
        totalCost: Number(raw.totalCost),
        notes: raw.notes || null,
      })
      .subscribe({
        next: () => {
          this.savingPaper.set(false);
          this.paperForm.patchValue({ notes: '' });
          this.reload();
        },
        error: () => this.savingPaper.set(false),
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
        purchaseDate: new Date(raw.purchaseDate).toISOString(),
        unitsCount: Number(raw.unitsCount),
        totalCost: Number(raw.totalCost),
        notes: raw.notes || null,
      })
      .subscribe({
        next: () => {
          this.savingUsb.set(false);
          this.usbForm.patchValue({ notes: '' });
          this.reload();
        },
        error: () => this.savingUsb.set(false),
      });
  }

  protected removeUsbPurchase(id: string): void {
    if (!window.confirm('¿Borrar esta compra de USBs?')) {
      return;
    }
    this.api.deleteUsbPurchase(id).subscribe({
      next: () => this.reload(),
    });
  }

  protected saveCostPerKm(): void {
    const value = Number(this.costPerKmInput());
    if (Number.isNaN(value) || value < 0) {
      return;
    }
    this.api.updateFinanceSettings(value).subscribe();
  }

  protected submitGlobalExpense(): void {
    if (this.expenseForm.invalid) {
      return;
    }
    const raw = this.expenseForm.getRawValue();
    this.savingExpense.set(true);
    this.api
      .addGlobalExpense({
        expenseDate: new Date(raw.expenseDate).toISOString(),
        category: raw.category,
        description: raw.description,
        amount: Number(raw.amount),
      })
      .subscribe({
        next: (expense) => {
          this.savingExpense.set(false);
          this.globalExpenses.update((list) => [expense, ...list]);
          this.expenseForm.patchValue({ description: '', amount: '' });
          this.reload();
        },
        error: () => this.savingExpense.set(false),
      });
  }

  protected removePaperPurchase(id: string): void {
    if (!window.confirm('¿Borrar esta compra de papel?')) {
      return;
    }
    this.api.deletePaperPurchase(id).subscribe({
      next: () => this.reload(),
    });
  }

  protected removeGlobalExpense(id: string): void {
    if (!window.confirm('¿Borrar este gasto?')) {
      return;
    }
    this.api.deleteGlobalExpense(id).subscribe({
      next: () => {
        this.globalExpenses.update((list) => list.filter((e) => e.id !== id));
        this.reload();
      },
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.getFinanceDashboard().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
