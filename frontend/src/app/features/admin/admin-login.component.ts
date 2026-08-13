import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminAuthService } from '../../core/admin/admin-auth.service';
import { ApiClient } from '../../core/api/api-client.service';

/**
 * `/admin/login` — there's no dedicated login endpoint. The candidate password is stored
 * optimistically, then verified by making the real first admin call (`GET /api/events`); a 401
 * rolls it back. Simpler than threading a one-off password override through ApiClient just to
 * validate a string.
 */
@Component({
  selector: 'app-admin-login',
  imports: [ReactiveFormsModule],
  template: `
    <div class="mx-auto flex min-h-screen max-w-sm flex-col justify-center gap-6 px-6">
      <div>
        <h1 class="text-2xl font-bold">Acceso admin</h1>
        <p class="mt-1 text-sm text-white/50">Contraseña compartida — no es seguridad real, solo evita que cualquiera navegue por acá.</p>
      </div>

      @if (error()) {
        <div class="rounded-lg bg-red-600/90 p-3 text-sm text-white">{{ error() }}</div>
      }

      <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
        <label class="flex flex-col gap-1">
          <span class="text-sm text-white/70">Contraseña</span>
          <input
            type="password"
            formControlName="password"
            autocomplete="current-password"
            class="rounded-lg bg-white/10 px-4 py-2 outline-none focus:ring-2 focus:ring-brand-500"
          />
        </label>

        <button
          type="submit"
          [disabled]="form.invalid || submitting()"
          class="rounded-full bg-brand-500 px-6 py-3 font-semibold text-white disabled:opacity-30"
        >
          {{ submitting() ? 'Verificando…' : 'Entrar' }}
        </button>
      </form>
    </div>
  `,
})
export class AdminLoginComponent {
  private readonly adminAuth = inject(AdminAuthService);
  private readonly api = inject(ApiClient);
  private readonly router = inject(Router);

  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = new FormGroup({
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  submit(): void {
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.adminAuth.setPassword(this.form.controls.password.value); // optimistic — rolled back below on 401

    this.api.listEvents().subscribe({
      next: () => {
        this.submitting.set(false);
        void this.router.navigateByUrl('/admin/events');
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        this.adminAuth.clear();
        this.error.set(error.status === 401 ? 'Contraseña incorrecta.' : 'No se pudo verificar. Intentá de nuevo.');
      },
    });
  }
}
