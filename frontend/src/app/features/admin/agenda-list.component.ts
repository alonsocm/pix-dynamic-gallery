import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { localDateInputToIso } from '../../core/common/local-date';
import { AGENDA_STATUS_LABELS, AGENDA_STATUSES, AgendaDepositDto, AgendaEntryDto, AgendaStatus } from '../../core/models/agenda.model';
import { AdminEventDto } from '../../core/models/event.model';

interface AgendaFormControls {
  clientName: FormControl<string>;
  eventType: FormControl<string>;
  eventDate: FormControl<string>;
  contactPhone: FormControl<string>;
  contactEmail: FormControl<string>;
  location: FormControl<string>;
  notes: FormControl<string>;
  agreedPrice: FormControl<string>;
}

function emptyForm(): FormGroup<AgendaFormControls> {
  return new FormGroup<AgendaFormControls>({
    clientName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    eventType: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    eventDate: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contactPhone: new FormControl('', { nonNullable: true }),
    contactEmail: new FormControl('', { nonNullable: true }),
    location: new FormControl('', { nonNullable: true }),
    notes: new FormControl('', { nonNullable: true }),
    agreedPrice: new FormControl('', { nonNullable: true }),
  });
}

/** `/admin/agenda` — bookings/prospects, independent of the technical Event they may later turn into. */
@Component({
  selector: 'app-agenda-list',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6 flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold">Agenda</h1>
        <div class="flex gap-2">
          <a routerLink="/admin/events" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 🎪 Eventos </a>
          <a routerLink="/admin/finance" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 💰 Finanzas </a>
          <a routerLink="/admin/inventory" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70"> 📦 Inventario </a>
          <button type="button" (click)="startCreate()" class="rounded-full bg-brand-600 px-4 py-2 text-sm font-semibold text-white">
            + Nueva reserva
          </button>
        </div>
      </div>

      @if (formOpen()) {
        <form [formGroup]="form" (ngSubmit)="submit()" class="mb-6 flex flex-col gap-3 rounded-lg bg-white/10 p-4">
          <h2 class="font-semibold">{{ editingId() ? 'Editar reserva' : 'Nueva reserva' }}</h2>

          <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <label class="flex flex-col gap-1">
              <span class="text-sm text-white/70">Cliente</span>
              <input formControlName="clientName" class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-sm text-white/70">Tipo de evento</span>
              <input
                formControlName="eventType"
                placeholder="Boda, XV años, Corporativo…"
                class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500"
              />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-sm text-white/70">Fecha del evento</span>
              <input
                type="datetime-local"
                formControlName="eventDate"
                class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500"
              />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-sm text-white/70">Precio pactado</span>
              <input
                type="number"
                min="0"
                step="0.01"
                formControlName="agreedPrice"
                class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500"
              />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-sm text-white/70">Teléfono</span>
              <input formControlName="contactPhone" class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1">
              <span class="text-sm text-white/70">Email</span>
              <input formControlName="contactEmail" class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1 sm:col-span-2">
              <span class="text-sm text-white/70">Ubicación</span>
              <input formControlName="location" class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500" />
            </label>
            <label class="flex flex-col gap-1 sm:col-span-2">
              <span class="text-sm text-white/70">Notas</span>
              <textarea formControlName="notes" rows="2" class="rounded-lg bg-white/10 px-3 py-2 outline-none focus:ring-2 focus:ring-brand-500"></textarea>
            </label>
          </div>

          @if (error()) {
            <div class="rounded-lg bg-red-600/90 p-3 text-sm text-white">{{ error() }}</div>
          }

          <div class="flex gap-2">
            <button
              type="submit"
              [disabled]="form.invalid || saving()"
              class="rounded-full bg-brand-600 px-5 py-2 text-sm font-semibold text-white disabled:opacity-30"
            >
              {{ saving() ? 'Guardando…' : editingId() ? 'Guardar cambios' : 'Crear reserva' }}
            </button>
            <button type="button" (click)="closeForm()" class="rounded-full bg-white/10 px-5 py-2 text-sm font-semibold text-white/70">
              Cancelar
            </button>
          </div>
        </form>
      }

      @if (loading()) {
        <p class="py-16 text-center text-white/50">Cargando…</p>
      } @else if (entries().length === 0) {
        <p class="py-16 text-center text-white/50">Todavía no hay reservas en la agenda.</p>
      } @else {
        <div class="flex flex-col gap-3">
          @for (entry of entries(); track entry.id) {
            <div class="rounded-lg bg-white/10 p-4">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <h2 class="truncate text-lg font-semibold">{{ entry.clientName }}</h2>
                  <p class="text-sm text-white/50">{{ entry.eventType }} · {{ formatDate(entry.eventDate) }}</p>
                </div>
                <span
                  class="shrink-0 rounded-full px-3 py-1 text-xs font-semibold"
                  [class.bg-brand-600]="entry.status === AgendaStatus.Confirmed"
                  [class.text-white]="entry.status === AgendaStatus.Confirmed"
                  [class.bg-white/10]="entry.status !== AgendaStatus.Confirmed"
                  [class.text-white/50]="entry.status !== AgendaStatus.Confirmed"
                >
                  {{ statusLabel(entry.status) }}
                </span>
              </div>

              @if (entry.location) {
                <p class="mt-1 text-xs text-white/40">📍 {{ entry.location }}</p>
              }
              @if (entry.agreedPrice !== null) {
                <p class="mt-1 text-xs text-white/40">💵 Precio pactado: {{ formatMoney(entry.agreedPrice) }}</p>
              }
              @if (entry.contactPhone || entry.contactEmail) {
                <p class="mt-1 text-xs text-white/40">
                  {{ entry.contactPhone }} {{ entry.contactPhone && entry.contactEmail ? '·' : '' }} {{ entry.contactEmail }}
                </p>
              }
              @if (entry.linkedEventId) {
                <p class="mt-1 text-xs text-brand-500">🔗 Vinculado a un evento técnico</p>
              }

              <!-- Anticipos / depósitos -->
              <div class="mt-3 rounded-lg bg-black/20 p-3">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-semibold text-white/70">💵 Anticipos</span>
                  <span class="text-xs text-white/50">Total: {{ formatMoney(entry.totalDeposited) }}</span>
                </div>

                @if (entry.deposits.length > 0) {
                  <ul class="mt-2 flex flex-col gap-1 text-xs text-white/60">
                    @for (dep of entry.deposits; track dep.id) {
                      <li class="flex items-center justify-between gap-2">
                        <span>
                          {{ formatDate(dep.paymentDate) }} — {{ formatMoney(dep.amount) }}
                          @if (dep.notes) { · {{ dep.notes }} }
                          @if (dep.transferredTransactionId) {
                            <span class="text-brand-500">(ya es ingreso del evento)</span>
                          }
                        </span>
                        @if (!dep.transferredTransactionId) {
                          <button type="button" (click)="removeDeposit(entry, dep)" aria-label="Borrar anticipo" class="text-white/30 hover:text-red-300">🗑️</button>
                        }
                      </li>
                    }
                  </ul>
                }

                @if (addingDepositFor() === entry.id) {
                  <div class="mt-2 flex flex-wrap items-end gap-2">
                    <label class="flex flex-col gap-1">
                      <span class="text-[10px] text-white/50">Monto</span>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        [value]="depositAmount()"
                        (input)="depositAmount.set($any($event.target).value)"
                        class="w-24 rounded-lg bg-white/10 px-2 py-1 text-xs outline-none focus:ring-2 focus:ring-brand-500"
                      />
                    </label>
                    <label class="flex flex-col gap-1">
                      <span class="text-[10px] text-white/50">Fecha</span>
                      <input
                        type="date"
                        [value]="depositDate()"
                        (input)="depositDate.set($any($event.target).value)"
                        class="rounded-lg bg-white/10 px-2 py-1 text-xs outline-none focus:ring-2 focus:ring-brand-500"
                      />
                    </label>
                    <label class="flex flex-1 flex-col gap-1">
                      <span class="text-[10px] text-white/50">Nota</span>
                      <input
                        [value]="depositNotes()"
                        (input)="depositNotes.set($any($event.target).value)"
                        placeholder="Efectivo, transferencia…"
                        class="rounded-lg bg-white/10 px-2 py-1 text-xs outline-none focus:ring-2 focus:ring-brand-500"
                      />
                    </label>
                    <button
                      type="button"
                      [disabled]="!depositAmount() || savingDeposit()"
                      (click)="submitDeposit(entry)"
                      class="rounded-full bg-brand-600 px-3 py-1 text-xs font-semibold text-white disabled:opacity-30"
                    >
                      Guardar
                    </button>
                    <button type="button" (click)="addingDepositFor.set(null)" class="text-xs text-white/40">Cancelar</button>
                  </div>
                } @else {
                  <button type="button" (click)="startAddDeposit(entry)" class="mt-2 text-xs font-semibold text-brand-500">
                    + Registrar anticipo
                  </button>
                }
              </div>

              <div class="mt-3 flex flex-wrap gap-2">
                @for (status of statuses; track status) {
                  @if (status !== entry.status) {
                    <button
                      type="button"
                      (click)="changeStatus(entry, status)"
                      class="rounded-full bg-white/10 px-3 py-1.5 text-xs font-semibold text-white"
                    >
                      → {{ statusLabel(status) }}
                    </button>
                  }
                }
                <button type="button" (click)="startEdit(entry)" class="rounded-full bg-white/10 px-3 py-1.5 text-xs font-semibold text-white">
                  ✏️ Editar
                </button>
                @if (!entry.linkedEventId) {
                  <button
                    type="button"
                    (click)="linkToEvent(entry)"
                    class="rounded-full bg-white/10 px-3 py-1.5 text-xs font-semibold text-white"
                  >
                    🔗 Vincular a evento
                  </button>
                }
                <button type="button" (click)="remove(entry)" class="rounded-full bg-white/10 px-3 py-1.5 text-xs font-semibold text-red-300">
                  🗑️ Borrar
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class AgendaListComponent implements OnInit {
  private readonly api = inject(ApiClient);

  /** Exposed so the template can compare against enum members, e.g. `entry.status === AgendaStatus.Confirmed`. */
  protected readonly AgendaStatus = AgendaStatus;
  protected readonly statuses = AGENDA_STATUSES;
  protected readonly entries = signal<AgendaEntryDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly formOpen = signal(false);
  protected readonly editingId = signal<string | null>(null);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected form = emptyForm();

  protected readonly addingDepositFor = signal<string | null>(null);
  protected readonly depositAmount = signal('');
  protected readonly depositDate = signal(new Date().toISOString().slice(0, 10));
  protected readonly depositNotes = signal('');
  protected readonly savingDeposit = signal(false);

  private events: AdminEventDto[] = [];

  ngOnInit(): void {
    this.reload();
  }

  protected statusLabel(status: AgendaStatus): string {
    return AGENDA_STATUS_LABELS[status];
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleString('es-MX', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  protected formatMoney(value: number): string {
    return value.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' });
  }

  protected startCreate(): void {
    this.editingId.set(null);
    this.form = emptyForm();
    this.error.set(null);
    this.formOpen.set(true);
  }

  protected startEdit(entry: AgendaEntryDto): void {
    this.editingId.set(entry.id);
    this.form = emptyForm();
    this.form.setValue({
      clientName: entry.clientName,
      eventType: entry.eventType,
      eventDate: entry.eventDate.slice(0, 16),
      contactPhone: entry.contactPhone ?? '',
      contactEmail: entry.contactEmail ?? '',
      location: entry.location ?? '',
      notes: entry.notes ?? '',
      agreedPrice: entry.agreedPrice !== null ? String(entry.agreedPrice) : '',
    });
    this.error.set(null);
    this.formOpen.set(true);
  }

  protected closeForm(): void {
    this.formOpen.set(false);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const request = {
      clientName: raw.clientName,
      eventType: raw.eventType,
      eventDate: new Date(raw.eventDate).toISOString(),
      contactPhone: raw.contactPhone || null,
      contactEmail: raw.contactEmail || null,
      location: raw.location || null,
      notes: raw.notes || null,
      agreedPrice: raw.agreedPrice ? Number(raw.agreedPrice) : null,
    };

    this.saving.set(true);
    this.error.set(null);

    const id = this.editingId();
    const request$ = id ? this.api.updateAgendaEntry(id, request) : this.api.createAgendaEntry(request);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.formOpen.set(false);
        this.reload();
      },
      error: () => {
        this.saving.set(false);
        this.error.set('No se pudo guardar la reserva. Intentá de nuevo.');
      },
    });
  }

  protected changeStatus(entry: AgendaEntryDto, status: AgendaStatus): void {
    this.api.setAgendaStatus(entry.id, status).subscribe({
      next: (updated) => this.entries.update((list) => list.map((e) => (e.id === updated.id ? updated : e))),
    });
  }

  protected linkToEvent(entry: AgendaEntryDto): void {
    this.api.listEvents().subscribe({
      next: (events) => {
        this.events = events;
        const options = events.map((e, i) => `${i + 1}) ${e.name} (${e.slug})`).join('\n');
        const choice = window.prompt(`¿A qué evento técnico querés vincular esta reserva?\n${options}\n\nEscribí el número:`);
        const index = choice ? Number(choice) - 1 : -1;
        const target = this.events[index];
        if (!target) {
          return;
        }
        this.api.linkAgendaEntryToEvent(entry.id, target.id).subscribe({
          next: (updated) => this.entries.update((list) => list.map((e) => (e.id === updated.id ? updated : e))),
        });
      },
    });
  }

  protected remove(entry: AgendaEntryDto): void {
    if (!window.confirm(`¿Borrar la reserva de "${entry.clientName}"? Esta acción no se puede deshacer.`)) {
      return;
    }
    this.api.deleteAgendaEntry(entry.id).subscribe({
      next: () => this.entries.update((list) => list.filter((e) => e.id !== entry.id)),
    });
  }

  protected startAddDeposit(entry: AgendaEntryDto): void {
    this.addingDepositFor.set(entry.id);
    this.depositAmount.set('');
    this.depositDate.set(new Date().toISOString().slice(0, 10));
    this.depositNotes.set('');
  }

  protected submitDeposit(entry: AgendaEntryDto): void {
    const amount = Number(this.depositAmount());
    if (!amount || amount <= 0) {
      return;
    }

    this.savingDeposit.set(true);
    this.api
      .addAgendaDeposit(entry.id, {
        amount,
        paymentDate: localDateInputToIso(this.depositDate()),
        notes: this.depositNotes() || null,
      })
      .subscribe({
        next: () => {
          this.savingDeposit.set(false);
          this.addingDepositFor.set(null);
          this.reload();
        },
        error: () => this.savingDeposit.set(false),
      });
  }

  protected removeDeposit(entry: AgendaEntryDto, deposit: AgendaDepositDto): void {
    if (!window.confirm('¿Borrar este anticipo?')) {
      return;
    }
    this.api.deleteAgendaDeposit(entry.id, deposit.id).subscribe({
      next: () => this.reload(),
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.api.listAgendaEntries().subscribe({
      next: (entries) => {
        this.entries.set(entries);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
