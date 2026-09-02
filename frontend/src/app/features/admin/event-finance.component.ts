import { Component, OnInit, effect, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { EventDto } from '../../core/models/event.model';
import {
  EXPENSE_CATEGORIES,
  EventFinanceSummaryDto,
  EventTransactionDto,
  FINANCE_CATEGORY_LABELS,
  FinanceCategory,
  FinanceTransactionType,
  INCOME_CATEGORIES,
} from '../../core/models/finance.model';

interface TransactionFormControls {
  category: FormControl<FinanceCategory>;
  description: FormControl<string>;
  amount: FormControl<string>;
  transactionDate: FormControl<string>;
}

function todayLocalDate(): string {
  return new Date().toISOString().slice(0, 10);
}

/** `/admin/events/:eventId/finance` — one event's income/expense ledger. `:eventId` is the slug, resolved via eventResolver like event-photos.component.ts. */
@Component({
  selector: 'app-event-finance',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6">
        <a routerLink="/admin/events" class="text-sm text-white/50">← Eventos</a>
        <h1 class="text-2xl font-bold text-white/90">{{ event().name }} — Finanzas</h1>
      </div>

      @if (loading()) {
        <p class="py-16 text-center text-white/50">Cargando…</p>
      } @else if (summary(); as s) {
        <div class="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div class="rounded-lg bg-white/10 p-4">
            <p class="text-xs text-white/50">Ingresos</p>
            <p class="mt-1 text-xl font-bold text-emerald-400">{{ formatMoney(s.totalIncome) }}</p>
          </div>
          <div class="rounded-lg bg-white/10 p-4">
            <p class="text-xs text-white/50">Gastos</p>
            <p class="mt-1 text-xl font-bold text-red-400">{{ formatMoney(s.totalExpense) }}</p>
          </div>
          <div class="rounded-lg bg-white/10 p-4">
            <p class="text-xs text-white/50">Ganancia</p>
            <p class="mt-1 text-xl font-bold" [class.text-emerald-400]="s.profit >= 0" [class.text-red-400]="s.profit < 0">
              {{ formatMoney(s.profit) }}
            </p>
          </div>
        </div>

        <!-- Acciones rápidas: fotos y gasolina auto-calculados -->
        <div class="mb-6 flex flex-col gap-3 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">Gastos calculados</h2>

          <div class="flex flex-wrap items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Fotos ({{ s.suggestedPhotoCount }} sugeridas)</span>
              <input type="number" min="0" [value]="photoCountInput()" (input)="photoCountInput.set($any($event.target).value)" class="w-24 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Costo/foto</span>
              <input type="number" min="0" step="0.01" [value]="costPerPhotoInput()" (input)="costPerPhotoInput.set($any($event.target).value)" class="w-24 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <button type="button" [disabled]="addingPhotoExpense()" (click)="addPhotoExpense()" class="rounded-full bg-white/10 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              📸 Agregar gasto de fotos
            </button>
          </div>

          <div class="flex flex-wrap items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Distancia (km)</span>
              <input type="number" min="0" step="0.1" [value]="distanceKmInput()" (input)="distanceKmInput.set($any($event.target).value)" class="w-24 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Costo/km</span>
              <input type="number" min="0" step="0.01" [value]="costPerKmInput()" (input)="costPerKmInput.set($any($event.target).value)" class="w-24 rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <button type="button" [disabled]="addingGasolineExpense() || !distanceKmInput()" (click)="addGasolineExpense()" class="rounded-full bg-white/10 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              ⛽ Agregar gasto de gasolina
            </button>
          </div>
        </div>

        <!-- Movimiento manual -->
        <form [formGroup]="form" (ngSubmit)="submitManual()" class="mb-6 flex flex-col gap-3 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">Movimiento manual</h2>
          <div class="flex flex-wrap items-end gap-2">
            <div class="flex gap-1">
              <button type="button" (click)="setType(FinanceTransactionType.Income)" [class.bg-brand-500]="type() === FinanceTransactionType.Income" [class.bg-white/10]="type() !== FinanceTransactionType.Income" class="rounded-full px-4 py-1.5 text-sm font-semibold text-white">
                Ingreso
              </button>
              <button type="button" (click)="setType(FinanceTransactionType.Expense)" [class.bg-brand-500]="type() === FinanceTransactionType.Expense" [class.bg-white/10]="type() !== FinanceTransactionType.Expense" class="rounded-full px-4 py-1.5 text-sm font-semibold text-white">
                Gasto
              </button>
            </div>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Categoría</span>
              <select formControlName="category" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500">
                @for (c of categoriesForType(); track c) {
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
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Fecha</span>
              <input type="date" formControlName="transactionDate" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <button type="submit" [disabled]="form.invalid || savingManual()" class="rounded-full bg-brand-500 px-5 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
              + Agregar
            </button>
          </div>
        </form>

        <!-- Lista de transacciones -->
        @if (s.transactions.length === 0) {
          <p class="py-8 text-center text-white/40">Todavía no hay movimientos para este evento.</p>
        } @else {
          <ul class="flex flex-col gap-2">
            @for (t of s.transactions; track t.id) {
              <li class="flex items-center justify-between gap-2 rounded-lg bg-white/10 p-3 text-sm">
                <div class="min-w-0">
                  <p class="truncate text-white/80">
                    {{ categoryLabel(t.category) }} @if (t.description) { · {{ t.description }} } @if (t.isAutoCalculated) { <span class="text-white/30">(auto)</span> }
                  </p>
                  <p class="text-xs text-white/40">{{ formatDate(t.transactionDate) }}</p>
                </div>
                <div class="flex items-center gap-2">
                  <span [class.text-emerald-400]="t.type === FinanceTransactionType.Income" [class.text-red-400]="t.type === FinanceTransactionType.Expense" class="font-semibold">
                    {{ t.type === FinanceTransactionType.Income ? '+' : '−' }}{{ formatMoney(t.amount) }}
                  </span>
                  <button type="button" (click)="removeTransaction(t)" class="text-xs text-white/40 hover:text-red-300">🗑️</button>
                </div>
              </li>
            }
          </ul>
        }
      }
    </div>
  `,
})
export class EventFinanceComponent implements OnInit {
  private readonly api = inject(ApiClient);

  readonly event = input.required<EventDto>();

  /** Exposed so the template can compare against/pass enum members. */
  protected readonly FinanceTransactionType = FinanceTransactionType;

  protected readonly loading = signal(true);
  protected readonly summary = signal<EventFinanceSummaryDto | null>(null);
  protected readonly type = signal<FinanceTransactionType>(FinanceTransactionType.Expense);

  protected readonly photoCountInput = signal('0');
  protected readonly costPerPhotoInput = signal('0');
  protected readonly distanceKmInput = signal('');
  protected readonly costPerKmInput = signal('0');
  protected readonly addingPhotoExpense = signal(false);
  protected readonly addingGasolineExpense = signal(false);
  protected readonly savingManual = signal(false);

  protected readonly form = new FormGroup<TransactionFormControls>({
    category: new FormControl<FinanceCategory>(FinanceCategory.Other, { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    amount: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] }),
    transactionDate: new FormControl(todayLocalDate(), { nonNullable: true, validators: [Validators.required] }),
  });

  constructor() {
    effect(() => {
      const ev = this.event();
      if (ev) {
        this.reload(ev.id);
      }
    });
  }

  ngOnInit(): void {
    // reload() runs from the `event` effect above once the resolved input is available.
  }

  protected categoriesForType(): FinanceCategory[] {
    return this.type() === FinanceTransactionType.Income ? INCOME_CATEGORIES : EXPENSE_CATEGORIES;
  }

  protected setType(type: FinanceTransactionType): void {
    this.type.set(type);
    this.form.patchValue({ category: this.categoriesForType()[0] });
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

  protected addPhotoExpense(): void {
    this.addingPhotoExpense.set(true);
    this.api
      .addPhotoExpense(this.event().id, {
        photoCount: this.photoCountInput() ? Number(this.photoCountInput()) : null,
        costPerPhoto: this.costPerPhotoInput() ? Number(this.costPerPhotoInput()) : null,
      })
      .subscribe({
        next: () => {
          this.addingPhotoExpense.set(false);
          this.reload(this.event().id);
        },
        error: () => this.addingPhotoExpense.set(false),
      });
  }

  protected addGasolineExpense(): void {
    const distanceKm = Number(this.distanceKmInput());
    if (!distanceKm || distanceKm <= 0) {
      return;
    }
    this.addingGasolineExpense.set(true);
    this.api
      .addGasolineExpense(this.event().id, {
        distanceKm,
        costPerKm: this.costPerKmInput() ? Number(this.costPerKmInput()) : null,
      })
      .subscribe({
        next: () => {
          this.addingGasolineExpense.set(false);
          this.distanceKmInput.set('');
          this.reload(this.event().id);
        },
        error: () => this.addingGasolineExpense.set(false),
      });
  }

  protected submitManual(): void {
    if (this.form.invalid) {
      return;
    }
    const raw = this.form.getRawValue();
    this.savingManual.set(true);
    this.api
      .addEventTransaction(this.event().id, {
        type: this.type(),
        category: raw.category,
        description: raw.description || null,
        amount: Number(raw.amount),
        transactionDate: new Date(raw.transactionDate).toISOString(),
      })
      .subscribe({
        next: () => {
          this.savingManual.set(false);
          this.form.patchValue({ description: '', amount: '' });
          this.reload(this.event().id);
        },
        error: () => this.savingManual.set(false),
      });
  }

  protected removeTransaction(transaction: EventTransactionDto): void {
    if (!window.confirm('¿Borrar este movimiento?')) {
      return;
    }
    this.api.deleteEventTransaction(this.event().id, transaction.id).subscribe({
      next: () => this.reload(this.event().id),
    });
  }

  private reload(eventId: string): void {
    this.loading.set(true);
    this.api.getEventFinanceSummary(eventId).subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.photoCountInput.set(String(summary.suggestedPhotoCount));
        this.costPerPhotoInput.set(String(summary.suggestedCostPerPhoto));
        this.costPerKmInput.set(String(summary.suggestedCostPerKm));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
