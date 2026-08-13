import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AdminAuthService } from '../../core/admin/admin-auth.service';
import { ApiClient } from '../../core/api/api-client.service';
import { buildGuestWallUrl } from '../../core/event/guest-photo-url';
import { AdminEventDto } from '../../core/models/event.model';

/** `/admin/events` — every event, with quick access to its Wall/Kiosk, its guest link, and its active toggle. */
@Component({
  selector: 'app-events-list',
  imports: [RouterLink],
  template: `
    <div class="mx-auto max-w-3xl px-4 py-8 sm:px-6">
      <div class="mb-6 flex items-center justify-between gap-4">
        <h1 class="text-2xl font-bold">Eventos</h1>
        <div class="flex gap-2">
          <a routerLink="/admin/events/new" class="rounded-full bg-brand-500 px-4 py-2 text-sm font-semibold text-white">
            + Nuevo evento
          </a>
          <button type="button" (click)="logout()" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70">
            Salir
          </button>
        </div>
      </div>

      @if (loading()) {
        <p class="py-16 text-center text-white/50">Cargando…</p>
      } @else if (error()) {
        <div class="rounded-lg bg-red-600/90 p-3 text-sm text-white">{{ error() }}</div>
      } @else if (events().length === 0) {
        <p class="py-16 text-center text-white/50">Todavía no hay eventos creados.</p>
      } @else {
        <div class="flex flex-col gap-4">
          @for (event of events(); track event.id) {
            <div class="rounded-lg bg-white/10 p-4">
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0">
                  <h2 class="truncate text-lg font-semibold">{{ event.name }}</h2>
                  <p class="font-mono text-xs text-white/50">{{ event.slug }}</p>
                </div>
                <span
                  class="shrink-0 rounded-full px-3 py-1 text-xs font-semibold"
                  [class.bg-brand-500]="event.isActive"
                  [class.text-white]="event.isActive"
                  [class.bg-white/10]="!event.isActive"
                  [class.text-white/50]="!event.isActive"
                >
                  {{ event.isActive ? 'Activo' : 'Inactivo' }}
                </span>
              </div>

              <p class="mt-2 truncate font-mono text-xs text-white/40" [title]="event.watchFolderPath">
                📁 {{ event.watchFolderPath }}
              </p>
              <p class="mt-1 text-xs text-white/40">
                {{ event.photoCount }} foto{{ event.photoCount === 1 ? '' : 's' }} · creado el {{ formatDate(event.createdAtUtc) }}
              </p>

              <div class="mt-4 flex flex-wrap gap-2">
                <a [routerLink]="['/kiosk', event.slug]" class="rounded-full bg-white/10 px-3 py-1.5 text-sm font-semibold text-white">
                  🖥️ Kiosk
                </a>
                <a [routerLink]="['/e', event.slug, 'wall']" class="rounded-full bg-white/10 px-3 py-1.5 text-sm font-semibold text-white">
                  🧱 Muro
                </a>
                <a
                  [routerLink]="['/admin/events', event.slug, 'photos']"
                  class="rounded-full bg-white/10 px-3 py-1.5 text-sm font-semibold text-white"
                >
                  🖼️ Fotos
                </a>
                <button
                  type="button"
                  (click)="copyLink(event)"
                  class="rounded-full bg-white/10 px-3 py-1.5 text-sm font-semibold text-white"
                >
                  {{ copiedSlug() === event.slug ? '✅ ¡Copiado!' : '🔗 Copiar link' }}
                </button>
                <button
                  type="button"
                  [disabled]="togglingIds().has(event.id)"
                  (click)="toggleActive(event)"
                  class="rounded-full bg-white/10 px-3 py-1.5 text-sm font-semibold text-white disabled:opacity-40"
                >
                  {{ event.isActive ? '⏸️ Desactivar' : '▶️ Activar' }}
                </button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class EventsListComponent implements OnInit {
  private readonly api = inject(ApiClient);
  private readonly adminAuth = inject(AdminAuthService);
  private readonly router = inject(Router);

  protected readonly events = signal<AdminEventDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly copiedSlug = signal<string | null>(null);
  protected readonly togglingIds = signal<ReadonlySet<string>>(new Set());

  ngOnInit(): void {
    this.api.listEvents().subscribe({
      next: (events) => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudo cargar la lista de eventos.');
      },
    });
  }

  protected formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('es-MX', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  protected copyLink(event: AdminEventDto): void {
    void navigator.clipboard.writeText(buildGuestWallUrl(event)).then(() => {
      this.copiedSlug.set(event.slug);
      setTimeout(() => this.copiedSlug.set(null), 2000);
    });
  }

  protected toggleActive(event: AdminEventDto): void {
    this.togglingIds.update((ids) => new Set(ids).add(event.id));

    this.api.setEventActive(event.id, !event.isActive).subscribe({
      next: (updated) => {
        this.events.update((list) => list.map((e) => (e.id === updated.id ? { ...e, isActive: updated.isActive } : e)));
        this.togglingIds.update((ids) => {
          const next = new Set(ids);
          next.delete(event.id);
          return next;
        });
      },
      error: () => {
        this.togglingIds.update((ids) => {
          const next = new Set(ids);
          next.delete(event.id);
          return next;
        });
      },
    });
  }

  protected logout(): void {
    this.adminAuth.clear();
    void this.router.navigateByUrl('/admin/login');
  }
}
