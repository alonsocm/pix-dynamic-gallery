import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AdminAuthService } from '../../core/admin/admin-auth.service';

interface AdminNavItem {
  path: string;
  label: string;
  /** Dashboard needs `exact` so it doesn't also light up while on /admin/events, etc. */
  exact?: boolean;
}

// Just one entry on purpose: the Dashboard is the single hub for switching between
// Eventos/Agenda/Finanzas/Inventario (their own cards link out), so this bar doesn't repeat that
// same list of modules a second time. From any module page, this pill is the way back to the hub.
const NAV_ITEMS: AdminNavItem[] = [{ path: '/admin', label: '🏠 Dashboard', exact: true }];

/**
 * Persistent shell for every guarded `/admin/*` route: one top bar (brand, a pill back to the
 * Dashboard hub with an active-state highlight, logout) wrapping a `<router-outlet>`. Replaces
 * the pill row that used to be hand-copied at the top of
 * events-list/agenda-list/finance-dashboard/inventory-dashboard — this one lives in a single
 * place and doesn't re-render on every navigation.
 */
@Component({
  selector: 'app-admin-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="min-h-screen">
      <header class="flex flex-wrap items-center justify-between gap-3 bg-white/5 px-4 py-3 sm:px-6 print:hidden">
        <span class="text-lg font-bold text-white/90">PIX</span>

        <nav class="flex flex-wrap gap-2">
          @for (item of navItems; track item.path) {
            <a
              [routerLink]="item.path"
              routerLinkActive
              #rla="routerLinkActive"
              [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
              class="rounded-full px-4 py-2 text-sm font-semibold transition"
              [class.bg-brand-600]="rla.isActive"
              [class.text-white]="rla.isActive"
              [class.bg-white/10]="!rla.isActive"
              [class.text-white/70]="!rla.isActive"
            >
              {{ item.label }}
            </a>
          }
        </nav>

        <button type="button" (click)="logout()" class="rounded-full bg-white/10 px-4 py-2 text-sm font-semibold text-white/70">
          Salir
        </button>
      </header>

      <router-outlet />
    </div>
  `,
})
export class AdminShellComponent {
  private readonly adminAuth = inject(AdminAuthService);
  private readonly router = inject(Router);

  protected readonly navItems = NAV_ITEMS;

  protected logout(): void {
    this.adminAuth.clear();
    void this.router.navigateByUrl('/admin/login');
  }
}
