import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit
} from '@angular/core';
import { NgClass, DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Product } from '../../models/product.model';
import { ProductFormModalComponent } from '../../components/product-form-modal/product-form-modal.component';
import { debounceTime, distinctUntilChanged, Subject, switchMap, startWith } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [NgClass, DecimalPipe, DatePipe, FormsModule, ProductFormModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="animate-in">

      <!-- Page Header -->
      <div class="page-header flex items-start justify-between">
        <div>
          <h1 class="page-title">
            <span class="text-gradient">Produtos</span>
          </h1>
          <p class="page-subtitle">Gerencie o catálogo e o saldo em estoque</p>
        </div>
        <button (click)="openCreate()" id="btn-new-product" class="btn-primary">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          Novo Produto
        </button>
      </div>

      <!-- Stats Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        <div class="glass-card p-5">
          <p class="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-1">Total de Produtos</p>
          <p class="text-3xl font-bold text-white">{{ products().length }}</p>
        </div>
        <div class="glass-card p-5">
          <p class="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-1">Em Estoque</p>
          <p class="text-3xl font-bold text-success">{{ productsInStock() }}</p>
        </div>
        <div class="glass-card p-5">
          <p class="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-1">Sem Estoque</p>
          <p class="text-3xl font-bold text-danger">{{ productsOutOfStock() }}</p>
        </div>
      </div>

      <!-- Search + Table -->
      <div class="glass-card overflow-hidden">

        <!-- Search Bar -->
        <div class="p-5 border-b border-surface-800">
          <div class="relative max-w-sm">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-500"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0"/>
            </svg>
            <input id="search-products" type="text" [(ngModel)]="searchTerm"
                   (ngModelChange)="onSearch($event)"
                   placeholder="Buscar por código ou descrição..."
                   class="form-input pl-9"/>
          </div>
        </div>

        <!-- Loading -->
        @if (loading()) {
          <div class="flex items-center justify-center py-16 gap-3">
            <span class="spinner w-6 h-6"></span>
            <span class="text-surface-400 text-sm">Carregando produtos...</span>
          </div>
        }

        <!-- Empty State -->
        @else if (filteredProducts().length === 0) {
          <div class="flex flex-col items-center justify-center py-16 text-center gap-3">
            <div class="w-16 h-16 rounded-2xl bg-surface-800 flex items-center justify-center mb-2">
              <svg class="w-8 h-8 text-surface-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                      d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>
              </svg>
            </div>
            <p class="text-surface-300 font-semibold">Nenhum produto encontrado</p>
            <p class="text-surface-500 text-sm">
              {{ searchTerm ? 'Tente outro termo de busca.' : 'Clique em "Novo Produto" para começar.' }}
            </p>
          </div>
        }

        <!-- Table -->
        @else {
          <div class="overflow-x-auto">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Descrição</th>
                  <th>Saldo Estoque</th>
                  <th>Unidade</th>
                  <th>Atualizado em</th>
                  <th class="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                @for (product of filteredProducts(); track product.id) {
                  <tr>
                    <td>
                      <span class="font-mono text-primary-300 font-semibold text-xs bg-primary-950/60
                                   px-2.5 py-1 rounded-lg border border-primary-800/40">
                        {{ product.code }}
                      </span>
                    </td>
                    <td class="font-medium">{{ product.description }}</td>
                    <td>
                      <span [ngClass]="product.stockBalance > 0 ? 'badge-open' : 'badge-cancelled'">
                        <span class="w-1.5 h-1.5 rounded-full"
                              [ngClass]="product.stockBalance > 0 ? 'bg-success' : 'bg-danger'">
                        </span>
                        {{ product.stockBalance | number:'1.2-2' }}
                      </span>
                    </td>
                    <td class="text-surface-400">{{ product.unit }}</td>
                    <td class="text-surface-400 text-xs">{{ product.updatedAt | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td>
                      <div class="flex items-center justify-end gap-2">
                        <button (click)="openEdit(product)"
                                class="w-8 h-8 flex items-center justify-center rounded-lg text-surface-400
                                       hover:text-primary-400 hover:bg-primary-950/50 transition-all duration-150">
                          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                  d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
                          </svg>
                        </button>
                        <button (click)="confirmDelete(product)"
                                class="w-8 h-8 flex items-center justify-center rounded-lg text-surface-400
                                       hover:text-danger hover:bg-danger/10 transition-all duration-150">
                          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                  d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="px-5 py-3 border-t border-surface-800 text-xs text-surface-500">
            {{ filteredProducts().length }} produto(s) encontrado(s)
          </div>
        }
      </div>
    </div>

    <!-- Modals -->
    @if (showModal()) {
      <app-product-form-modal
        [product]="selectedProduct()"
        [loading]="saving()"
        (save)="onSave($event)"
        (cancel)="closeModal()"
      />
    }

    <!-- Delete Confirm Dialog -->
    @if (showDeleteConfirm()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" (click)="showDeleteConfirm.set(false)"></div>
        <div class="relative glass-card p-8 max-w-sm w-full animate-slide-up text-center">
          <div class="w-14 h-14 rounded-2xl bg-danger/10 flex items-center justify-center mx-auto mb-5">
            <svg class="w-7 h-7 text-danger" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
            </svg>
          </div>
          <h4 class="text-lg font-bold text-white mb-2">Excluir Produto</h4>
          <p class="text-surface-400 text-sm mb-7">
            Tem certeza que deseja excluir o produto
            <strong class="text-white">{{ productToDelete()?.code }}</strong>?
            Esta ação não pode ser desfeita.
          </p>
          <div class="flex gap-3">
            <button (click)="showDeleteConfirm.set(false)" class="btn-secondary flex-1">Cancelar</button>
            <button (click)="deleteProduct()" [disabled]="saving()" class="btn-danger flex-1">
              @if (saving()) { <span class="spinner w-4 h-4"></span> } @else { Excluir }
            </button>
          </div>
        </div>
      </div>
    }
  `
})
export class ProductsPageComponent implements OnInit {
  private productService = inject(ProductService);
  private toast = inject(ToastService);

  // State
  products     = signal<Product[]>([]);
  loading      = signal(false);
  saving       = signal(false);
  showModal    = signal(false);
  showDeleteConfirm = signal(false);
  selectedProduct   = signal<Product | null>(null);
  productToDelete   = signal<Product | null>(null);
  searchTerm        = '';

  private searchSubject = new Subject<string>();

  // Computed
  filteredProducts  = computed(() => {
    const term = this.searchTerm.toLowerCase();
    if (!term) return this.products();
    return this.products().filter(p =>
      p.code.toLowerCase().includes(term) ||
      p.description.toLowerCase().includes(term)
    );
  });

  productsInStock    = computed(() => this.products().filter(p => p.stockBalance > 0).length);
  productsOutOfStock = computed(() => this.products().filter(p => p.stockBalance <= 0).length);

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService.getAll().subscribe({
      next: products => { this.products.set(products); this.loading.set(false); },
      error: ()      => { this.loading.set(false); }
    });
  }

  onSearch(term: string): void {
    this.searchTerm = term;
  }

  openCreate(): void {
    this.selectedProduct.set(null);
    this.showModal.set(true);
  }

  openEdit(product: Product): void {
    this.selectedProduct.set(product);
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.selectedProduct.set(null);
  }

  onSave(formValue: any): void {
    this.saving.set(true);
    const isEdit = !!this.selectedProduct();

    const obs = isEdit
      ? this.productService.update(this.selectedProduct()!.id, formValue)
      : this.productService.create(formValue);

    obs.subscribe({
      next: () => {
        this.toast.success(
          isEdit ? 'Produto atualizado com sucesso!' : 'Produto cadastrado com sucesso!',
          isEdit ? 'Atualizado' : 'Cadastrado'
        );
        this.saving.set(false);
        this.closeModal();
        this.loadProducts();
      },
      error: () => { this.saving.set(false); }
    });
  }

  confirmDelete(product: Product): void {
    this.productToDelete.set(product);
    this.showDeleteConfirm.set(true);
  }

  deleteProduct(): void {
    if (!this.productToDelete()) return;
    this.saving.set(true);
    this.productService.delete(this.productToDelete()!.id).subscribe({
      next: () => {
        this.toast.success('Produto excluído com sucesso!', 'Excluído');
        this.saving.set(false);
        this.showDeleteConfirm.set(false);
        this.productToDelete.set(null);
        this.loadProducts();
      },
      error: () => { this.saving.set(false); }
    });
  }
}
