import {
  Component, Input, Output, EventEmitter, OnInit,
  ChangeDetectionStrategy, inject, signal, computed
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, FormArray, FormGroup } from '@angular/forms';
import { NgIf, NgFor, CurrencyPipe, DecimalPipe } from '@angular/common';
import { Product } from '../../../inventory/models/product.model';
import { ProductService } from '../../../inventory/services/product.service';
import { Subject, debounceTime, distinctUntilChanged, switchMap, startWith, Observable, map } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateInvoiceRequest, CreateInvoiceItemRequest } from '../../models/invoice.model';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-invoice-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule, NgIf, NgFor, CurrencyPipe, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Overlay -->
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
         (click)="onOverlayClick($event)">
      <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"></div>

      <!-- Modal -->
      <div class="relative w-full max-w-4xl max-h-[90vh] glass-card p-8 animate-slide-up overflow-y-auto"
           (click)="$event.stopPropagation()">

        <!-- Header -->
        <div class="flex items-center justify-between mb-7">
          <div>
            <h3 class="text-xl font-bold text-white">Nova Nota Fiscal</h3>
            <p class="text-sm text-surface-400 mt-0.5">Preencha os dados da NF e adicione os itens</p>
          </div>
          <button (click)="cancel.emit()"
                  class="w-9 h-9 flex items-center justify-center rounded-xl text-surface-400
                         hover:text-white hover:bg-surface-800 transition-all duration-150">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <!-- Form -->
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-6">

          <!-- General Info -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label for="customerName" class="form-label">Cliente (Opcional)</label>
              <input id="customerName" formControlName="customerName" type="text" class="form-input"
                     placeholder="Nome do cliente"/>
            </div>
            <div>
              <label for="notes" class="form-label">Observações</label>
              <input id="notes" formControlName="notes" type="text" class="form-input"
                     placeholder="Informações adicionais"/>
            </div>
          </div>

          <hr class="border-surface-800" />

          <!-- Items Section -->
          <div>
            <div class="flex items-center justify-between mb-4">
              <h4 class="text-lg font-semibold text-white">Itens da NF</h4>
              <button type="button" (click)="addItem()" class="btn-secondary text-sm py-1.5 px-3">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
                </svg>
                Adicionar Item
              </button>
            </div>

            <!-- Error if no items -->
            @if (itemsFormArray.errors?.['minItems'] && form.touched) {
              <div class="p-3 mb-4 rounded-lg bg-danger/10 text-danger text-sm border border-danger/20 flex items-center gap-2">
                <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/></svg>
                A nota fiscal deve ter ao menos um item.
              </div>
            }

            <!-- Items List -->
            <div formArrayName="items" class="space-y-3">
              @for (itemForm of itemsFormArray.controls; track i; let i = $index) {
                <div [formGroupName]="i" class="flex flex-col md:flex-row gap-3 p-4 rounded-xl bg-surface-900 border border-surface-800 relative">
                  
                  <!-- Product Search/Select -->
                  <div class="flex-1 relative">
                    <label class="form-label text-xs">Produto <span class="text-danger">*</span></label>
                    <div class="relative">
                      <input type="text"
                             [value]="selectedProducts()[i]?.description || ''"
                             (input)="onSearchProduct($event, i)"
                             (focus)="focusedItemIndex.set(i)"
                             (blur)="onBlurProductSearch(i)"
                             class="form-input"
                             placeholder="Buscar produto..."/>
                      
                      <!-- Dropdown -->
                      @if (focusedItemIndex() === i && productSearchResults().length > 0) {
                        <ul class="absolute z-10 w-full mt-1 max-h-48 overflow-y-auto bg-surface-800 border border-surface-700 rounded-lg shadow-xl">
                          @for (prod of productSearchResults(); track prod.id) {
                            <li (mousedown)="selectProduct(prod, i)"
                                class="px-3 py-2 text-sm hover:bg-surface-700 cursor-pointer flex justify-between items-center transition-colors">
                              <span>{{ prod.code }} - {{ prod.description }}</span>
                              <span class="text-xs text-surface-400">Estoque: {{ prod.stockBalance }}</span>
                            </li>
                          }
                        </ul>
                      }
                    </div>
                  </div>

                  <!-- Quantity -->
                  <div class="w-full md:w-24">
                    <label class="form-label text-xs">Qtd <span class="text-danger">*</span></label>
                    <input type="number" formControlName="quantity" class="form-input" min="0.01" step="0.01" placeholder="0"/>
                  </div>

                  <!-- Unit Price -->
                  <div class="w-full md:w-32">
                    <label class="form-label text-xs">Preço Unit. <span class="text-danger">*</span></label>
                    <div class="relative">
                      <span class="absolute left-3 top-1/2 -translate-y-1/2 text-surface-400">R$</span>
                      <input type="number" formControlName="unitPrice" class="form-input pl-8" min="0" step="0.01" placeholder="0.00"/>
                    </div>
                  </div>

                  <!-- Total Price (Readonly) -->
                  <div class="w-full md:w-32">
                    <label class="form-label text-xs">Total</label>
                    <div class="form-input bg-surface-950 text-surface-300 border-surface-800 flex items-center">
                      {{ calculateItemTotal(i) | currency:'BRL' }}
                    </div>
                  </div>

                  <!-- Remove Item -->
                  <div class="flex items-end pb-1">
                    <button type="button" (click)="removeItem(i)" 
                            class="w-10 h-10 flex items-center justify-center rounded-lg text-surface-500 hover:text-danger hover:bg-danger/10 transition-colors"
                            title="Remover item">
                      <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
                      </svg>
                    </button>
                  </div>
                </div>
              }
            </div>

            <!-- Total NF -->
            @if (itemsFormArray.length > 0) {
              <div class="flex justify-end mt-4">
                <div class="glass-card p-4 flex items-center gap-4">
                  <span class="text-surface-400 font-medium">Total da Nota Fiscal:</span>
                  <span class="text-2xl font-bold text-primary-400">{{ calculateInvoiceTotal() | currency:'BRL' }}</span>
                </div>
              </div>
            }
          </div>

          <!-- Actions -->
          <div class="flex gap-3 pt-4 mt-6 border-t border-surface-800">
            <button type="button" (click)="cancel.emit()" class="btn-secondary flex-1">
              Cancelar
            </button>
            <button type="submit" [disabled]="form.invalid || loading" class="btn-primary flex-1">
              @if (loading) {
                <span class="spinner w-4 h-4"></span>
                Salvando...
              } @else {
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                </svg>
                Gerar Nota Fiscal
              }
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class InvoiceFormModalComponent implements OnInit {
  @Input() loading = false;
  @Output() save = new EventEmitter<CreateInvoiceRequest>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private toast = inject(ToastService);

  // Search State
  private searchSubject = new Subject<{term: string, index: number}>();
  productSearchResults = signal<Product[]>([]);
  focusedItemIndex = signal<number | null>(null);
  
  // Track selected products to show their names in the input
  selectedProducts = signal<Record<number, Product>>({});

  form = this.fb.group({
    customerName: [''],
    notes: [''],
    items: this.fb.array([], [this.minLengthArray(1)])
  });

  get itemsFormArray() {
    return this.form.get('items') as FormArray;
  }

  constructor() {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged((prev, curr) => prev.term === curr.term),
      switchMap(query => this.productService.getAll(query.term)),
      takeUntilDestroyed()
    ).subscribe(products => {
      this.productSearchResults.set(products);
    });
  }

  ngOnInit(): void {
    this.addItem(); // Start with one empty item
  }

  // Custom validator for FormArray minimum length
  minLengthArray(min: number) {
    return (c: AbstractControl): {[key: string]: any} | null => {
      if (c.value.length >= min) return null;
      return { minItems: true };
    }
  }

  addItem(): void {
    const itemGroup = this.fb.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
    });
    this.itemsFormArray.push(itemGroup);
  }

  removeItem(index: number): void {
    this.itemsFormArray.removeAt(index);
    const updatedSelected = { ...this.selectedProducts() };
    delete updatedSelected[index];
    
    // Shift selected products if needed
    const newSelected: Record<number, Product> = {};
    Object.keys(updatedSelected).forEach(key => {
      const numKey = Number(key);
      if (numKey > index) {
        newSelected[numKey - 1] = updatedSelected[numKey];
      } else if (numKey < index) {
        newSelected[numKey] = updatedSelected[numKey];
      }
    });
    this.selectedProducts.set(newSelected);
  }

  onSearchProduct(event: Event, index: number): void {
    const term = (event.target as HTMLInputElement).value;
    
    // If they clear the input, clear the selected product for this index
    if (!term) {
      const itemControl = this.itemsFormArray.at(index);
      itemControl.patchValue({ productId: '', unitPrice: 0 });
      
      const updated = { ...this.selectedProducts() };
      delete updated[index];
      this.selectedProducts.set(updated);
    }
    
    this.searchSubject.next({term, index});
  }

  selectProduct(product: Product, index: number): void {
    const itemControl = this.itemsFormArray.at(index);
    itemControl.patchValue({
      productId: product.id,
      unitPrice: 0
    });

    const updated = { ...this.selectedProducts() };
    updated[index] = product;
    this.selectedProducts.set(updated);
    
    this.focusedItemIndex.set(null); // Close dropdown
  }

  onBlurProductSearch(index: number): void {
    // Timeout to allow mousedown event on list item to fire before closing dropdown
    setTimeout(() => {
      if (this.focusedItemIndex() === index) {
        this.focusedItemIndex.set(null);
      }
    }, 200);
  }

  calculateItemTotal(index: number): number {
    const group = this.itemsFormArray.at(index) as FormGroup;
    const qty = group.get('quantity')?.value || 0;
    const price = group.get('unitPrice')?.value || 0;
    return qty * price;
  }

  calculateInvoiceTotal(): number {
    let total = 0;
    for (let i = 0; i < this.itemsFormArray.length; i++) {
      total += this.calculateItemTotal(i);
    }
    return total;
  }

  submit(): void {
    if (this.form.valid) {
      const val = this.form.value;
      const request: CreateInvoiceRequest = {
        customerName: val.customerName ?? undefined,
        notes: val.notes ?? undefined,
        items: (val.items ?? []).map((i: any) => ({
          productId: i.productId,
          quantity: i.quantity,
          unitPrice: i.unitPrice
        }))
      };
      this.save.emit(request);
    } else {
      this.form.markAllAsTouched();
    }
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement) === event.currentTarget) {
      this.cancel.emit();
    }
  }
}
