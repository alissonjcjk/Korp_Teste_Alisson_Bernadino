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
  imports: [ReactiveFormsModule, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-form-modal.component.html'
})
export class InvoiceFormModalComponent implements OnInit {
  @Input() loading = false;
  @Output() save = new EventEmitter<CreateInvoiceRequest>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private toast = inject(ToastService);

  // Search State
  private searchSubject = new Subject<{ term: string, index: number }>();
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
    return (c: AbstractControl): { [key: string]: any } | null => {
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

    this.searchSubject.next({ term, index });
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
