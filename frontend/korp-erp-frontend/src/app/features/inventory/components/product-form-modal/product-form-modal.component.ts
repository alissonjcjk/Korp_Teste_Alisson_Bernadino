import {
  Component, Input, Output, EventEmitter, OnInit,
  ChangeDetectionStrategy, inject
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { decimalPrecision } from '../../../../core/validators/decimal.validator';
import { CreateProductRequest, Product } from '../../models/product.model';

@Component({
  selector: 'app-product-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-form-modal.component.html'
})
export class ProductFormModalComponent implements OnInit {
  @Input() product: Product | null = null;
  @Input() loading = false;
  @Output() save = new EventEmitter<CreateProductRequest>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder).nonNullable;

  form = this.fb.group({
    code:         ['', [Validators.required, Validators.maxLength(50)]],
    description:  ['', [Validators.required, Validators.maxLength(255)]],
    stockBalance: [0,  [Validators.required, Validators.min(0), decimalPrecision(14, 4)]],
    unit:         ['UN', [Validators.required, Validators.maxLength(20)]]
  });

  get f(): { [key: string]: AbstractControl } { return this.form.controls; }

  ngOnInit(): void {
    if (this.product) {
      this.form.patchValue({
        code:         this.product.code,
        description:  this.product.description,
        stockBalance: this.product.stockBalance,
        unit:         this.product.unit,
      });
      this.form.controls.code.disable();
      this.form.controls.stockBalance.disable();
    }
  }

  submit(): void {
    if (this.form.valid) this.save.emit(this.form.getRawValue());
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement) === event.currentTarget) {
      this.cancel.emit();
    }
  }
}
