import {
  Component, Input, Output, EventEmitter, OnInit,
  ChangeDetectionStrategy, inject, signal, computed
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, FormArray, FormGroup } from '@angular/forms';
import { CurrencyPipe } from '@angular/common';
import { Product } from '../../../inventory/models/product.model';
import { ProductService } from '../../../inventory/services/product.service';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateInvoiceRequest, CreateInvoiceItemRequest } from '../../models/invoice.model';
import { ToastService } from '../../../../core/services/toast.service';
import { decimalPrecision } from '../../../../core/validators/decimal.validator';

@Component({
  selector: 'app-invoice-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoice-form-modal.component.html'
})
export class InvoiceFormModalComponent implements OnInit {
  readonly maxInvoiceItems = 100;

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
  selectedProducts = signal<Partial<Record<number, Product>>>({});

  // numeric(18,4): em JavaScript, usamos o limite superior exclusivo porque
  // 99.999.999.999.999,9999 não é representável exatamente como number.
  private readonly storedAmountUpperBound = 100_000_000_000_000;

  form = this.fb.group({
    customerName: ['', Validators.maxLength(255)],
    notes: ['', Validators.maxLength(1000)],
    items: this.fb.array([], [
      this.minLengthArray(1),
      this.maxLengthArray(this.maxInvoiceItems),
      this.invoiceTotalWithinStoredPrecision()
    ])
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

  maxLengthArray(max: number) {
    return (c: AbstractControl): { maxItems: true } | null =>
      c.value.length <= max ? null : { maxItems: true };
  }

  addItem(): void {
    if (this.itemsFormArray.length >= this.maxInvoiceItems) {
      return;
    }

    const itemGroup = this.fb.group(
      {
        productId: ['', Validators.required],
        quantity: [1, [Validators.required, Validators.min(0.0001), decimalPrecision(14, 4)]],
        unitPrice: [0, [Validators.required, Validators.min(0), decimalPrecision(14, 4)]]
      },
      { validators: [this.lineTotalWithinStoredPrecision()] }
    );
    this.itemsFormArray.push(itemGroup);
  }

  removeItem(index: number): void {
    this.itemsFormArray.removeAt(index);
    const updatedSelected = { ...this.selectedProducts() };
    delete updatedSelected[index];

    // Shift selected products if needed
    const newSelected: Partial<Record<number, Product>> = {};
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
    return this.roundLineTotal(qty, price);
  }

  calculateInvoiceTotal(): number {
    let total = 0;
    for (let i = 0; i < this.itemsFormArray.length; i++) {
      total += this.calculateItemTotal(i);
    }
    return total;
  }

  private lineTotalWithinStoredPrecision() {
    return (control: AbstractControl): { lineTotalPrecision: true } | null => {
      const quantity = Number(control.get('quantity')?.value);
      const unitPrice = Number(control.get('unitPrice')?.value);

      if (!Number.isFinite(quantity) || !Number.isFinite(unitPrice) || quantity <= 0 || unitPrice < 0) {
        return null;
      }

      return this.roundLineTotal(quantity, unitPrice) < this.storedAmountUpperBound
        ? null
        : { lineTotalPrecision: true };
    };
  }

  private invoiceTotalWithinStoredPrecision() {
    return (control: AbstractControl): { invoiceTotalPrecision: true } | null => {
      const items = Array.isArray(control.value) ? control.value : [];
      let total = 0;

      for (const item of items) {
        const quantity = Number(item?.quantity);
        const unitPrice = Number(item?.unitPrice);
        if (!Number.isFinite(quantity) || !Number.isFinite(unitPrice) || quantity <= 0 || unitPrice < 0) {
          return null;
        }

        const lineTotal = this.roundLineTotal(quantity, unitPrice);
        if (lineTotal >= this.storedAmountUpperBound) {
          return null;
        }

        total += lineTotal;
        if (total >= this.storedAmountUpperBound) {
          return { invoiceTotalPrecision: true };
        }
      }

      return null;
    };
  }

  private roundLineTotal(quantity: number, unitPrice: number): number {
    const scaleFactor = 10 ** 4;
    return Math.round(quantity * unitPrice * scaleFactor) / scaleFactor;
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
