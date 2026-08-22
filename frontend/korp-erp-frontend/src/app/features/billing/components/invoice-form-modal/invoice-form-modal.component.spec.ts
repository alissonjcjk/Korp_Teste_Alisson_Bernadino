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
