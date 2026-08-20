import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-products-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="animate-in">
      <div class="page-header">
        <h1 class="page-title">
          <span class="text-gradient">Produtos</span>
        </h1>
        <p class="page-subtitle">Gerencie o catálogo de produtos e o saldo em estoque</p>
      </div>
      <div class="glass-card p-8 text-center text-surface-400">
        <p>Etapa 8 — Em breve!</p>
      </div>
    </div>
  `
})
export class ProductsPageComponent {}
