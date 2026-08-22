import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit
} from '@angular/core';
import { NgClass, DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Product } from '../../models/product.model';
import { ProductFormModalComponent } from '../../components/product-form-modal/product-form-modal.component';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [NgClass, DecimalPipe, DatePipe, FormsModule, ProductFormModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './products-page.component.html'
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
