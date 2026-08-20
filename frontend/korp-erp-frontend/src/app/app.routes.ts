import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/produtos',
    pathMatch: 'full'
  },
  {
    path: 'produtos',
    loadComponent: () =>
      import('./features/inventory/pages/products-page/products-page.component')
        .then(m => m.ProductsPageComponent),
    title: 'Produtos — Korp ERP'
  },
  {
    path: 'notas-fiscais',
    loadComponent: () =>
      import('./features/billing/pages/invoices-page/invoices-page.component')
        .then(m => m.InvoicesPageComponent),
    title: 'Notas Fiscais — Korp ERP'
  },
  {
    path: '**',
    redirectTo: '/produtos'
  }
];
