import { Injectable, computed, signal } from '@angular/core';

const STORAGE_KEY = 'pix-admin-password';

/**
 * Wraps the shared admin password in a signal, persisted to localStorage (not sessionStorage —
 * this is a solo-operator convenience tool: optimize for "don't re-enter every tab session," not
 * for security; see AdminAuthAttribute on the backend for why this isn't real protection).
 *
 * There's no dedicated login endpoint — AdminLoginComponent "verifies" a candidate password by
 * making the real first admin call (GET /api/events) with it attached, and rolls back on a 401.
 */
@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly _password = signal<string | null>(localStorage.getItem(STORAGE_KEY));
  readonly password = this._password.asReadonly();
  readonly isAuthenticated = computed(() => this._password() !== null);

  setPassword(password: string): void {
    localStorage.setItem(STORAGE_KEY, password);
    this._password.set(password);
  }

  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._password.set(null);
  }
}
