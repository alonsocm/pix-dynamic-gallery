import { Component } from '@angular/core';

@Component({
  selector: 'app-not-found',
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-2 px-6 text-center">
      <span class="text-6xl">🔍</span>
      <h1 class="text-2xl font-bold">No encontramos esa página</h1>
      <p class="text-white/70">El evento o la foto que buscas no existe o el enlace es incorrecto.</p>
    </div>
  `,
})
export class NotFoundComponent {}
