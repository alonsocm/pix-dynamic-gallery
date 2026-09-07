import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { localDateInputToIso } from '../../core/common/local-date';
import {
  EXPENSE_CATEGORIES,
  FINANCE_CATEGORY_LABELS,
  FinanceCategory,
  FinanceDashboardDto,
  MonthlyAmountDto,
  MonthlyCountDto,
} from '../../core/models/finance.model';

interface GlobalExpenseFormControls {
  expenseDate: FormControl<string>;
  category: FormControl<FinanceCategory>;
  description: FormControl<string>;
  amount: FormControl<string>;
}

interface GlobalExpenseRow {
  id: string;
  expenseDate: string;
  category: FinanceCategory;
  description: string;
  amount: number;
}

/** One calendar month's worth of `GlobalExpenseRow`s, newest month first — same chunking idea as
 *  "Ingresos por mes" below, applied to a list that would otherwise render every row ever logged,
 *  flat, forever. */
interface GlobalExpenseMonthGroup {
  key: string;
  label: string;
  items: GlobalExpenseRow[];
  subtotal: number;
}

function todayLocalDate(): string {
  return new Date().toISOString().slice(0, 10);
}

const MONTH_LABELS = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

/** perEventBreakdown has no date to group by (it's already one row per event) — a flat cap plus
 *  "mostrar todos" is the simplest guard against it growing unscannable after years of events. */
const EVENT_BREAKDOWN_PAGE_SIZE = 8;

/** `/admin/finance` — balance and reports only (totals, per-event breakdown, monthly income/events, agenda deposits, global expenses, settings). Stock/inventory lives in /admin/inventory instead. */
@Component({
  selector: 'app-finance-dashboard',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6 flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold">Finanzas</h1>
        <div class="flex flex-wrap gap-2">
          <a routerLink="/admin/events" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 🎪 Eventos </a>
          <a routerLink="/admin/agenda" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 📅 Agenda </a>
          <a routerLink="/admin/inventory" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 📦 Inventario </a>
        </div>
      </div>

      @if (loading()) {
        <p class="py-16 text-center text-white/50">Cargando…</p>
      } @else if (dashboard(); as d) {
        @if (error()) {
          <div class="mb-4 rounded-lg bg-red-600/90 p-3 text-sm text-white">{{ error() }}</div>
        }

        <!-- Totales globales: la ganancia neta es la respuesta a "¿voy bien?", así que lidera con
             tipografía de Display a todo el ancho — ingresos/gastos son los insumos, no el titular. -->
        <div class="mb-6 flex flex-col gap-3">
          <div class="rounded-lg bg-white/10 p-5">
            <p class="text-xs text-white/50">Ganancia neta</p>
            <p class="mt-1 text-3xl font-bold" [class.text-emerald-400]="d.netProfit >= 0" [class.text-red-400]="d.netProfit < 0">
              {{ formatMoney(d.netProfit) }}
            </p>
          </div>
          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
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
          </div>
        </div>

        <!-- Reportes: ingresos por mes / eventos por mes -->
        <div class="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-2">
          <section class="rounded-lg bg-white/10 p-4">
            <h2 class="font-semibold">📈 Ingresos por mes</h2>
            @if (d.incomeByMonth.length === 0) {
              <p class="mt-2 text-sm text-white/40">Todavía no hay ingresos registrados.</p>
            } @else {
              <ul class="mt-3 flex flex-col gap-2">
                @for (m of d.incomeByMonth; track m.year + '-' + m.month) {
                  <li class="flex items-center gap-2">
                    <span class="w-14 shrink-0 text-xs text-white/50">{{ monthLabel(m.year, m.month) }}</span>
                    <span class="h-2 flex-1 overflow-hidden rounded-full bg-white/10">
                      <span class="block h-full rounded-full bg-emerald-400" [style.width.%]="barWidth(m.total, maxIncome(d))"></span>
                    </span>
                    <span class="w-20 shrink-0 text-right text-xs font-semibold text-white/80">{{ formatMoney(m.total) }}</span>
                  </li>
                }
              </ul>
            }
          </section>

          <section class="rounded-lg bg-white/10 p-4">
            <h2 class="font-semibold">📅 Eventos por mes</h2>
            <p class="mt-1 text-xs text-white/40">Reservas de agenda no canceladas, por fecha del evento.</p>
            @if (d.eventsByMonth.length === 0) {
              <p class="mt-2 text-sm text-white/40">Todavía no hay eventos agendados.</p>
            } @else {
              <ul class="mt-3 flex flex-col gap-2">
                @for (m of d.eventsByMonth; track m.year + '-' + m.month) {
                  <li class="flex items-center gap-2">
                    <span class="w-14 shrink-0 text-xs text-white/50">{{ monthLabel(m.year, m.month) }}</span>
                    <span class="h-2 flex-1 overflow-hidden rounded-full bg-white/10">
                      <span class="block h-full rounded-full bg-brand-500" [style.width.%]="barWidth(m.count, maxEvents(d))"></span>
                    </span>
                    <span class="w-8 shrink-0 text-right text-xs font-semibold text-white/80">{{ m.count }}</span>
                  </li>
                }
              </ul>
            }
          </section>
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
            <button
              type="button"
              (click)="saveCostPerKm()"
              [disabled]="savingCostPerKm()"
              class="rounded-full bg-white/10 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-40"
            >
              {{ savingCostPerKm() ? 'Guardando…' : costPerKmSaved() ? '✅ Guardado' : 'Guardar' }}
            </button>
          </div>
        </section>

        <!-- Gastos globales -->
        <section class="mb-6 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">🧾 Gastos globales</h2>
          <p class="mt-1 text-xs text-white/40">No atados a un evento específico (equipo, marketing, etc.).</p>

          @if (groupedExpenses().length > 0) {
            <div class="mt-3 flex flex-col gap-3">
              @for (group of groupedExpenses(); track group.key) {
                <div>
                  <div class="flex items-center justify-between text-xs font-semibold text-white/50">
                    <span>{{ group.label }}</span>
                    <span>{{ formatMoney(group.subtotal) }}</span>
                  </div>
                  <ul class="mt-1 flex flex-col gap-1 text-sm">
                    @for (e of group.items; track e.id) {
                      <li class="flex items-center justify-between gap-2 text-white/70">
                        <span>{{ formatDate(e.expenseDate) }} · {{ categoryLabel(e.category) }} · {{ e.description }}</span>
                        <span class="flex items-center gap-2">
                          <span class="text-red-400">{{ formatMoney(e.amount) }}</span>
                          <button type="button" (click)="removeGlobalExpense(e.id)" aria-label="Borrar gasto" class="text-xs text-white/40 hover:text-red-300">🗑️</button>
                        </span>
                      </li>
                    }
                  </ul>
                </div>
              }
            </div>
          }

          <form [formGroup]="expenseForm" (ngSubmit)="submitGlobalExpense()" class="mt-3 flex flex-wrap items-end gap-2">
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Fecha</span>
              <input type="date" formControlName="expenseDate" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-xs text-white/50">Categoría</span>
              <select formControlName="category" aria-label="Categoría" class="rounded-lg bg-white/10 px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-brand-500">
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
            <button type="submit" [disabled]="expenseForm.invalid || savingExpense()" class="rounded-full bg-brand-600 px-4 py-1.5 text-sm font-semibold text-white disabled:opacity-30">
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
              @for (b of visibleEventBreakdown(d); track b.eventId) {
                <li class="flex items-center justify-between gap-2">
                  <a [routerLink]="['/admin/events', b.eventId, 'finance']" class="truncate text-white/80 hover:underline">{{ b.eventName }}</a>
                  <span [class.text-emerald-400]="b.profit >= 0" [class.text-red-400]="b.profit < 0" class="font-semibold">
                    {{ formatMoney(b.profit) }}
                  </span>
                </li>
              }
            </ul>
            @if (d.perEventBreakdown.length > eventBreakdownPageSize) {
              <button type="button" (click)="showAllEvents.set(!showAllEvents())" class="mt-2 text-xs font-semibold text-brand-500">
                {{ showAllEvents() ? 'Mostrar menos' : 'Mostrar todos (' + d.perEventBreakdown.length + ')' }}
              </button>
            }
          }
        </section>
      } @else {
        <div class="rounded-lg bg-red-600/90 p-3 text-sm text-white">{{ error() ?? 'No se pudo cargar el dashboard de finanzas.' }}</div>
      }
    </div>
  `,
})
export class FinanceDashboardComponent implements OnInit {
  private readonly api = inject(ApiClient);

  protected readonly expenseCategories = EXPENSE_CATEGORIES;
  protected readonly eventBreakdownPageSize = EVENT_BREAKDOWN_PAGE_SIZE;
  protected readonly loading = signal(true);
  protected readonly dashboard = signal<FinanceDashboardDto | null>(null);
  protected readonly globalExpenses = signal<GlobalExpenseRow[]>([]);
  protected readonly costPerKmInput = signal('0');
  protected readonly savingExpense = signal(false);
  protected readonly savingCostPerKm = signal(false);
  protected readonly costPerKmSaved = signal(false);
  protected readonly showAllEvents = signal(false);
  protected readonly error = signal<string | null>(null);

  /** Chunks `globalExpenses` into month buckets, newest first — see GlobalExpenseMonthGroup. */
  protected readonly groupedExpenses = computed<GlobalExpenseMonthGroup[]>(() => {
    const groups = new Map<string, GlobalExpenseMonthGroup>();
    for (const expense of this.globalExpenses()) {
      const date = new Date(expense.expenseDate);
      const key = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
      let group = groups.get(key);
      if (!group) {
        group = { key, label: this.monthLabel(date.getFullYear(), date.getMonth() + 1), items: [], subtotal: 0 };
        groups.set(key, group);
      }
      group.items.push(expense);
      group.subtotal += expense.amount;
    }
    return [...groups.values()].sort((a, b) => (a.key < b.key ? 1 : -1));
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

  protected monthLabel(year: number, month: number): string {
    return `${MONTH_LABELS[month - 1]} ${year}`;
  }

  protected maxIncome(d: FinanceDashboardDto): number {
    return Math.max(1, ...d.incomeByMonth.map((m: MonthlyAmountDto) => m.total));
  }

  protected maxEvents(d: FinanceDashboardDto): number {
    return Math.max(1, ...d.eventsByMonth.map((m: MonthlyCountDto) => m.count));
  }

  protected barWidth(value: number, max: number): number {
    return max === 0 ? 0 : Math.max(2, Math.round((value / max) * 100));
  }

  protected visibleEventBreakdown(d: FinanceDashboardDto) {
    return this.showAllEvents() ? d.perEventBreakdown : d.perEventBreakdown.slice(0, EVENT_BREAKDOWN_PAGE_SIZE);
  }

  protected saveCostPerKm(): void {
    const value = Number(this.costPerKmInput());
    if (Number.isNaN(value) || value < 0) {
      this.error.set('El costo por km debe ser un número válido, mayor o igual a 0.');
      return;
    }
    this.error.set(null);
    this.savingCostPerKm.set(true);
    this.api.updateFinanceSettings(value).subscribe({
      next: () => {
        this.savingCostPerKm.set(false);
        this.costPerKmSaved.set(true);
        setTimeout(() => this.costPerKmSaved.set(false), 2000);
      },
      error: () => {
        this.savingCostPerKm.set(false);
        this.error.set('No se pudo guardar el costo por km. Intentá de nuevo.');
      },
    });
  }

  protected submitGlobalExpense(): void {
    if (this.expenseForm.invalid) {
      return;
    }
    const raw = this.expenseForm.getRawValue();
    this.savingExpense.set(true);
    this.error.set(null);
    this.api
      .addGlobalExpense({
        expenseDate: localDateInputToIso(raw.expenseDate),
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
        error: () => {
          this.savingExpense.set(false);
          this.error.set('No se pudo guardar el gasto. Intentá de nuevo.');
        },
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
      error: () => this.error.set('No se pudo borrar el gasto. Intentá de nuevo.'),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.getFinanceDashboard().subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudo cargar el dashboard de finanzas.');
      },
    });
  }
}
