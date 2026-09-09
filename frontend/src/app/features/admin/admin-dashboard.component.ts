import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiClient } from '../../core/api/api-client.service';
import { AgendaStatus } from '../../core/models/agenda.model';

interface DashboardCard {
  path: string;
  icon: string;
  title: string;
  loading: boolean;
  error: boolean;
  lines: string[];
}

const INITIAL_CARDS: DashboardCard[] = [
  { path: '/admin/events', icon: '🎪', title: 'Eventos', loading: true, error: false, lines: [] },
  { path: '/admin/agenda', icon: '📅', title: 'Agenda', loading: true, error: false, lines: [] },
  { path: '/admin/finance', icon: '💰', title: 'Finanzas', loading: true, error: false, lines: [] },
  { path: '/admin/inventory', icon: '📦', title: 'Inventario', loading: true, error: false, lines: [] },
];

/**
 * `/admin` — index route of the admin area: one card per module (Eventos/Agenda/Finanzas/
 * Inventario), each showing a live KPI pulled from the same endpoints those modules already
 * call. Every card loads independently — a slow/failed endpoint only affects its own card.
 */
@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <h1 class="mb-6 text-2xl font-bold">Dashboard</h1>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        @for (card of cards(); track card.path) {
          <div class="rounded-lg bg-white/10 p-5">
            <h2 class="text-lg font-semibold">{{ card.icon }} {{ card.title }}</h2>

            @if (card.loading) {
              <p class="mt-2 text-sm text-white/40">Cargando…</p>
            } @else if (card.error) {
              <p class="mt-2 text-sm text-red-400">No se pudo cargar.</p>
            } @else {
              <div class="mt-2 flex flex-col gap-1 text-sm text-white/70">
                @for (line of card.lines; track line) {
                  <p>{{ line }}</p>
                }
              </div>
            }

            <a [routerLink]="card.path" class="mt-4 inline-block rounded-full bg-white/10 px-4 py-1.5 text-sm font-semibold text-white">
              Ver →
            </a>
          </div>
        }
      </div>
    </div>
  `,
})
export class AdminDashboardComponent implements OnInit {
  private readonly api = inject(ApiClient);

  protected readonly cards = signal<DashboardCard[]>(INITIAL_CARDS);

  ngOnInit(): void {
    this.loadEvents();
    this.loadAgenda();
    this.loadFinance();
    this.loadInventory();
  }

  private updateCard(path: string, patch: Partial<DashboardCard>): void {
    this.cards.update((list) => list.map((c) => (c.path === path ? { ...c, ...patch } : c)));
  }

  private formatMoney(value: number): string {
    return value.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' });
  }

  private loadEvents(): void {
    this.api.listEvents().subscribe({
      next: (events) => {
        const active = events.filter((e) => e.isActive).length;
        const photos = events.reduce((sum, e) => sum + e.photoCount, 0);
        this.updateCard('/admin/events', {
          loading: false,
          lines: [
            `${active} evento${active === 1 ? '' : 's'} activo${active === 1 ? '' : 's'}`,
            `${photos} foto${photos === 1 ? '' : 's'} en total`,
          ],
        });
      },
      error: () => this.updateCard('/admin/events', { loading: false, error: true }),
    });
  }

  private loadAgenda(): void {
    this.api.listAgendaEntries().subscribe({
      next: (entries) => {
        const prospects = entries.filter((e) => e.status === AgendaStatus.Prospect).length;
        const now = Date.now();
        const upcoming = entries.filter((e) => e.status === AgendaStatus.Confirmed && new Date(e.eventDate).getTime() >= now).length;
        this.updateCard('/admin/agenda', {
          loading: false,
          lines: [
            `${upcoming} reserva${upcoming === 1 ? '' : 's'} confirmada${upcoming === 1 ? '' : 's'} próxima${upcoming === 1 ? '' : 's'}`,
            `${prospects} prospecto${prospects === 1 ? '' : 's'} sin confirmar`,
          ],
        });
      },
      error: () => this.updateCard('/admin/agenda', { loading: false, error: true }),
    });
  }

  private loadFinance(): void {
    this.api.getFinanceDashboard().subscribe({
      next: (d) => {
        const lines = [`Ganancia neta: ${this.formatMoney(d.netProfit)}`];
        if (d.pendingDepositsTotal > 0) {
          lines.push(`${this.formatMoney(d.pendingDepositsTotal)} en anticipos pendientes`);
        }
        this.updateCard('/admin/finance', { loading: false, lines });
      },
      error: () => this.updateCard('/admin/finance', { loading: false, error: true }),
    });
  }

  private loadInventory(): void {
    forkJoin({ paper: this.api.getPaperStock(), usb: this.api.getUsbStock() }).subscribe({
      next: ({ paper, usb }) => {
        this.updateCard('/admin/inventory', {
          loading: false,
          lines: [`${paper.remainingSheets} hojas restantes`, `${usb.remainingUnits} USBs restantes`],
        });
      },
      error: () => this.updateCard('/admin/inventory', { loading: false, error: true }),
    });
  }
}
