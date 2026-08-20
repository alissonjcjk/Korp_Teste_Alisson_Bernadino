import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-invoices-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="animate-in">
      <div class="page-header">
        <h1 class="page-title">
          <span class="text-gradient">Notas Fiscais</span>
        </h1>
        <p class="page-subtitle">Emita e gerencie notas fiscais</p>
      </div>
      <div class="glass-card p-8 text-center text-surface-400">
        <p>Etapa 9 — Em breve!</p>
      </div>
    </div>
  `
})
export class InvoicesPageComponent {}
