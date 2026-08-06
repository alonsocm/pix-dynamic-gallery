import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiClient } from '../../core/api/api-client.service';
import { EventDto, ValidationProblemDetails } from '../../core/models/event.model';

type RuntimeMode = 'docker' | 'native';

interface CreateEventFormControls {
  name: FormControl<string>;
  slug: FormControl<string>;
  watchFolderPath: FormControl<string>;
  guestBaseUrl: FormControl<string>;
}

const SLUG_PATTERN = /^[a-z0-9]+(-[a-z0-9]+)*$/;

function slugify(value: string): string {
  return value
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '') // strip accents (á->a, ñ->n, etc.)
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/** Validators.pattern rejects empty strings by default via a bad regex-anchoring gotcha, so a tiny custom one instead. */
function absoluteUrlValidator(): ValidatorFn {
  return (control): ValidationErrors | null => {
    if (!control.value) {
      return null; // required is a separate validator
    }
    try {
      new URL(control.value);
      return null;
    } catch {
      return { absoluteUrl: true };
    }
  };
}

/**
 * `/admin/events/new` — no auth (consistent with the rest of the system today, see README);
 * this is a convenience UI over `POST /api/events`, which already existed and is unchanged.
 */
@Component({
  selector: 'app-create-event',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="mx-auto flex min-h-screen max-w-lg flex-col justify-center gap-6 px-6 py-12">
      @if (createdEvent(); as created) {
        <div class="flex flex-col items-center gap-4 text-center">
          <span class="text-5xl">🎉</span>
          <h1 class="text-2xl font-bold">¡Evento creado!</h1>
          <p class="text-white/70">
            <strong>{{ created.name }}</strong> (<code class="text-brand-500">{{ created.slug }}</code>)
          </p>

          <div class="flex w-full flex-col gap-2 sm:flex-row">
            <a
              [routerLink]="['/kiosk', created.slug]"
              class="flex-1 rounded-full bg-white px-4 py-2 text-center font-semibold text-ink-900"
            >
              🖥️ Ir al Kiosk
            </a>
            <a
              [routerLink]="['/e', created.slug, 'wall']"
              class="flex-1 rounded-full bg-brand-500 px-4 py-2 text-center font-semibold text-white"
            >
              🧱 Ir al Muro
            </a>
          </div>

          <button type="button" (click)="reset()" class="text-sm text-white/50 underline">
            Crear otro evento
          </button>
        </div>
      } @else {
        <div>
          <h1 class="text-2xl font-bold">Crear evento</h1>
          <p class="mt-1 text-sm text-white/50">Sin login por ahora — es una herramienta de operador local.</p>
        </div>

        @if (generalError()) {
          <div class="rounded-lg bg-red-600/90 p-3 text-sm text-white">{{ generalError() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4">
          <label class="flex flex-col gap-1">
            <span class="text-sm text-white/70">Nombre del evento</span>
            <input
              type="text"
              formControlName="name"
              placeholder="Boda de Julia y Mark"
              class="rounded-lg bg-white/10 px-4 py-2 outline-none focus:ring-2 focus:ring-brand-500"
            />
            @if (form.controls.name.invalid && form.controls.name.touched) {
              <span class="text-xs text-red-400">{{ form.controls.name.errors?.['server'] || 'Requerido.' }}</span>
            }
          </label>

          <label class="flex flex-col gap-1">
            <span class="text-sm text-white/70">Slug (URL)</span>
            <input
              type="text"
              formControlName="slug"
              placeholder="julia-and-mark"
              class="rounded-lg bg-white/10 px-4 py-2 font-mono outline-none focus:ring-2 focus:ring-brand-500"
            />
            @if (form.controls.slug.invalid && form.controls.slug.touched) {
              <span class="text-xs text-red-400">
                {{ form.controls.slug.errors?.['server'] || 'Solo minúsculas, números y guiones (ej. "julia-and-mark").' }}
              </span>
            }
          </label>

          <div class="flex flex-col gap-2">
            <span class="text-sm text-white/70">¿Cómo corre la API ahora mismo?</span>
            <div class="flex gap-2">
              <button
                type="button"
                (click)="applyRuntimeMode('docker')"
                [class.bg-brand-500]="runtimeMode() === 'docker'"
                [class.bg-white/10]="runtimeMode() !== 'docker'"
                class="flex-1 rounded-lg px-3 py-2 text-sm font-semibold text-white"
              >
                🐳 Docker
              </button>
              <button
                type="button"
                (click)="applyRuntimeMode('native')"
                [class.bg-brand-500]="runtimeMode() === 'native'"
                [class.bg-white/10]="runtimeMode() !== 'native'"
                class="flex-1 rounded-lg px-3 py-2 text-sm font-semibold text-white"
              >
                🪟 Nativa en Windows
              </button>
            </div>
          </div>

          <label class="flex flex-col gap-1">
            <span class="text-sm text-white/70">Carpeta a vigilar</span>
            <input
              type="text"
              formControlName="watchFolderPath"
              placeholder="C:\SparkboothPhotos\julia-and-mark"
              class="rounded-lg bg-white/10 px-4 py-2 font-mono text-sm outline-none focus:ring-2 focus:ring-brand-500"
            />
            @if (form.controls.watchFolderPath.invalid && form.controls.watchFolderPath.touched) {
              <span class="text-xs text-red-400">{{ form.controls.watchFolderPath.errors?.['server'] || 'Requerido.' }}</span>
            }
          </label>

          <label class="flex flex-col gap-1">
            <span class="text-sm text-white/70">URL pública (para el QR)</span>
            <input
              type="text"
              formControlName="guestBaseUrl"
              class="rounded-lg bg-white/10 px-4 py-2 font-mono text-sm outline-none focus:ring-2 focus:ring-brand-500"
            />
            @if (form.controls.guestBaseUrl.invalid && form.controls.guestBaseUrl.touched) {
              <span class="text-xs text-red-400">
                {{ form.controls.guestBaseUrl.errors?.['server'] || 'Debe ser una URL absoluta, ej. "https://gallery.tuestudio.com".' }}
              </span>
            }
          </label>

          <button
            type="submit"
            [disabled]="form.invalid || submitting()"
            class="mt-2 rounded-full bg-brand-500 px-6 py-3 font-semibold text-white disabled:opacity-30"
          >
            {{ submitting() ? 'Creando…' : 'Crear evento' }}
          </button>
        </form>
      }
    </div>
  `,
})
export class CreateEventComponent {
  private readonly api = inject(ApiClient);

  protected readonly runtimeMode = signal<RuntimeMode>('docker');
  protected readonly submitting = signal(false);
  protected readonly generalError = signal<string | null>(null);
  protected readonly createdEvent = signal<EventDto | null>(null);

  private slugManuallyEdited = false;

  protected readonly form = new FormGroup<CreateEventFormControls>({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    slug: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(SLUG_PATTERN)],
    }),
    watchFolderPath: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    guestBaseUrl: new FormControl(typeof window !== 'undefined' ? window.location.origin : '', {
      nonNullable: true,
      validators: [Validators.required, absoluteUrlValidator()],
    }),
  });

  constructor() {
    // Auto-generate the slug from the name, but stop the moment the user types into the slug
    // field directly — never fight the user's own edit.
    this.form.controls.slug.valueChanges.subscribe(() => (this.slugManuallyEdited = true));
    this.form.controls.name.valueChanges.subscribe((name) => {
      if (!this.slugManuallyEdited) {
        this.form.controls.slug.setValue(slugify(name), { emitEvent: false });
      }
    });
  }

  /** Re-derives watchFolderPath's prefix from the current slug — a one-shot fill, not continuous sync. */
  applyRuntimeMode(mode: RuntimeMode): void {
    this.runtimeMode.set(mode);
    const slug = this.form.controls.slug.value || '{slug}';
    const prefix = mode === 'docker' ? '/data/sparkbooth' : 'C:\\SparkboothPhotos';
    const separator = mode === 'docker' ? '/' : '\\';
    this.form.controls.watchFolderPath.setValue(`${prefix}${separator}${slug}`);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.generalError.set(null);

    this.api.createEvent(this.form.getRawValue()).subscribe({
      next: (event) => {
        this.submitting.set(false);
        this.createdEvent.set(event);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting.set(false);
        this.applyServerErrors(error);
      },
    });
  }

  reset(): void {
    this.createdEvent.set(null);
    this.generalError.set(null);
    this.slugManuallyEdited = false;
    this.form.reset({
      name: '',
      slug: '',
      watchFolderPath: '',
      guestBaseUrl: typeof window !== 'undefined' ? window.location.origin : '',
    });
  }

  private applyServerErrors(error: HttpErrorResponse): void {
    const problem = error.error as ValidationProblemDetails | undefined;

    if (error.status === 400 && problem?.errors) {
      // Field names come back exactly as FluentValidation named them (the C# property name, e.g.
      // "Slug") — compare case-insensitively so this doesn't silently break if that ever changes.
      const byLowerName = new Map(Object.entries(problem.errors).map(([key, msgs]) => [key.toLowerCase(), msgs]));

      for (const controlName of Object.keys(this.form.controls) as Array<keyof CreateEventFormControls>) {
        const messages = byLowerName.get(controlName.toLowerCase());
        if (messages?.length) {
          this.form.controls[controlName].setErrors({ server: messages.join(' ') });
        }
      }
      this.form.markAllAsTouched();
      return;
    }

    this.generalError.set('No se pudo crear el evento. Intentá de nuevo en un momento.');
  }
}
