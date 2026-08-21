import {
  Component, Input, Output, EventEmitter, OnInit,
  ChangeDetectionStrategy, inject
} from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { Product } from '../../models/product.model';

@Component({
  selector: 'app-product-form-modal',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Overlay -->
    <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
         (click)="onOverlayClick($event)">
      <div class="absolute inset-0 bg-black/60 backdrop-blur-sm"></div>

      <!-- Modal -->
      <div class="relative w-full max-w-lg glass-card p-8 animate-slide-up"
           (click)="$event.stopPropagation()">

        <!-- Header -->
        <div class="flex items-center justify-between mb-7">
          <div>
            <h3 class="text-xl font-bold text-white">
              {{ product ? 'Editar Produto' : 'Novo Produto' }}
            </h3>
            <p class="text-sm text-surface-400 mt-0.5">
              {{ product ? 'Altere os dados do produto' : 'Preencha os dados para cadastrar' }}
            </p>
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
        <form [formGroup]="form" (ngSubmit)="submit()" class="space-y-5">

          <!-- Code -->
          <div>
            <label for="code" class="form-label">Código <span class="text-danger">*</span></label>
            <input id="code" formControlName="code" type="text" class="form-input"
                   placeholder="Ex: PROD-001" [attr.disabled]="product ? true : null"/>
            @if (f['code'].invalid && f['code'].touched) {
              <p class="form-error">
                <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                </svg>
                {{ f['code'].errors?.['required'] ? 'Código é obrigatório' : 'Máximo 50 caracteres' }}
              </p>
            }
          </div>

          <!-- Description -->
          <div>
            <label for="description" class="form-label">Descrição <span class="text-danger">*</span></label>
            <input id="description" formControlName="description" type="text" class="form-input"
                   placeholder="Descrição detalhada do produto"/>
            @if (f['description'].invalid && f['description'].touched) {
              <p class="form-error">
                <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                </svg>
                {{ f['description'].errors?.['required'] ? 'Descrição é obrigatória' : 'Máximo 255 caracteres' }}
              </p>
            }
          </div>

          <!-- Stock Balance + Unit (grid) -->
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label for="stockBalance" class="form-label">Saldo Estoque <span class="text-danger">*</span></label>
              <input id="stockBalance" formControlName="stockBalance" type="number" min="0" step="0.01"
                     class="form-input" placeholder="0"/>
              @if (f['stockBalance'].invalid && f['stockBalance'].touched) {
                <p class="form-error">
                  <svg class="w-3.5 h-3.5" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
                  </svg>
                  Valor inválido
                </p>
              }
            </div>
            <div>
              <label for="unit" class="form-label">Unidade</label>
              <select id="unit" formControlName="unit" class="form-input">
                <option value="UN">UN</option>
                <option value="KG">KG</option>
                <option value="LT">LT</option>
                <option value="MT">MT</option>
                <option value="CX">CX</option>
                <option value="PC">PC</option>
              </select>
            </div>
          </div>

          <!-- Actions -->
          <div class="flex gap-3 pt-2">
            <button type="button" (click)="cancel.emit()" class="btn-secondary flex-1">
              Cancelar
            </button>
            <button type="submit" [disabled]="form.invalid || loading"
                    class="btn-primary flex-1">
              @if (loading) {
                <span class="spinner w-4 h-4"></span>
                Salvando...
              } @else {
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
                </svg>
                {{ product ? 'Salvar Alterações' : 'Cadastrar' }}
              }
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class ProductFormModalComponent implements OnInit {
  @Input() product: Product | null = null;
  @Input() loading = false;
  @Output() save = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();

  private fb = inject(FormBuilder);

  form = this.fb.group({
    code:         ['', [Validators.required, Validators.maxLength(50)]],
    description:  ['', [Validators.required, Validators.maxLength(255)]],
    stockBalance: [0,  [Validators.required, Validators.min(0)]],
    unit:         ['UN']
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
    }
  }

  submit(): void {
    if (this.form.valid) this.save.emit(this.form.value);
  }

  onOverlayClick(event: MouseEvent): void {
    if ((event.target as HTMLElement) === event.currentTarget) {
      this.cancel.emit();
    }
  }
}
