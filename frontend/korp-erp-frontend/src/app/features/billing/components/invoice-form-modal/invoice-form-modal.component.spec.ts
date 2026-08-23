import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProductService } from '../../../inventory/services/product.service';
import { InvoiceFormModalComponent } from './invoice-form-modal.component';

describe('InvoiceFormModalComponent', () => {
  let fixture: ComponentFixture<InvoiceFormModalComponent>;
  let component: InvoiceFormModalComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceFormModalComponent],
      providers: [
        {
          provide: ProductService,
          useValue: { getAll: () => of([]) }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceFormModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('starts with one required invoice item', () => {
    expect(component.itemsFormArray.length).toBe(1);
    expect(component.form.valid).toBeFalse();
  });

  it('supports adding and removing invoice items', () => {
    component.addItem();
    expect(component.itemsFormArray.length).toBe(2);

    component.removeItem(0);
    expect(component.itemsFormArray.length).toBe(1);
  });

  it('does not allow adding more than one hundred items', () => {
    for (let index = component.itemsFormArray.length; index < component.maxInvoiceItems + 1; index++) {
      component.addItem();
    }

    expect(component.itemsFormArray.length).toBe(component.maxInvoiceItems);
  });

  it('validates quantity, price precision and optional text limits', () => {
    const item = component.itemsFormArray.at(0);

    item.patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 0.0001,
      unitPrice: 1.2345
    });
    expect(item.valid).toBeTrue();

    item.patchValue({ quantity: 0.00001, unitPrice: 1.23456 });
    expect(item.get('quantity')?.hasError('decimalPrecision')).toBeTrue();
    expect(item.get('unitPrice')?.hasError('decimalPrecision')).toBeTrue();

    item.patchValue({ quantity: 99999999999999, unitPrice: 1 });
    expect(item.valid).toBeTrue();

    item.patchValue({ quantity: 99999999999999, unitPrice: 2 });
    expect(item.hasError('lineTotalPrecision')).toBeTrue();

    item.patchValue({ quantity: 100000000000000, unitPrice: 100000000000000 });
    expect(item.get('quantity')?.hasError('decimalPrecision')).toBeTrue();
    expect(item.get('unitPrice')?.hasError('decimalPrecision')).toBeTrue();

    component.form.controls.customerName.setValue('C'.repeat(256));
    component.form.controls.notes.setValue('N'.repeat(1001));
    expect(component.form.controls.customerName.hasError('maxlength')).toBeTrue();
    expect(component.form.controls.notes.hasError('maxlength')).toBeTrue();
  });

  it('rejects an aggregate invoice total that does not fit numeric(18,4)', () => {
    const first = component.itemsFormArray.at(0);
    first.patchValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 1,
      unitPrice: 60000000000000
    });
    component.addItem();
    component.itemsFormArray.at(1).patchValue({
      productId: '22222222-2222-2222-2222-222222222222',
      quantity: 1,
      unitPrice: 60000000000000
    });

    expect(component.itemsFormArray.hasError('invoiceTotalPrecision')).toBeTrue();
    expect(component.form.valid).toBeFalse();
  });

  it('emits a valid invoice request', () => {
    spyOn(component.save, 'emit');
    component.form.controls.customerName.setValue('Cliente de teste');
    component.form.controls.notes.setValue('Observação');
    component.itemsFormArray.at(0).setValue({
      productId: '11111111-1111-1111-1111-111111111111',
      quantity: 2,
      unitPrice: 15
    });

    component.submit();

    expect(component.save.emit).toHaveBeenCalledWith({
      customerName: 'Cliente de teste',
      notes: 'Observação',
      items: [
        {
          productId: '11111111-1111-1111-1111-111111111111',
          quantity: 2,
          unitPrice: 15
        }
      ]
    });
  });
});
