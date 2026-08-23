import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/pages/home-page/home-page.component')
        .then(m => m.HomePageComponent),
    title: 'Home — Korp ERP'
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
