import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Product } from '../../models/product.model';
import { ProductFormModalComponent } from './product-form-modal.component';

describe('ProductFormModalComponent', () => {
  let fixture: ComponentFixture<ProductFormModalComponent>;
  let component: ProductFormModalComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductFormModalComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ProductFormModalComponent);
    component = fixture.componentInstance;
  });

  it('requires code, description and a non-negative stock balance', () => {
    expect(component.form.valid).toBeFalse();

    component.form.setValue({
      code: 'PROD-001',
      description: 'Produto de teste',
      stockBalance: 10,
      unit: 'UN'
    });

    expect(component.form.valid).toBeTrue();

    component.form.controls.stockBalance.setValue(-1);
    expect(component.form.controls.stockBalance.hasError('min')).toBeTrue();
  });

  it('emits the form value when a valid product is submitted', () => {
    spyOn(component.save, 'emit');
    component.form.setValue({
      code: 'PROD-001',
      description: 'Produto de teste',
      stockBalance: 10,
      unit: 'UN'
    });

    component.submit();

    expect(component.save.emit).toHaveBeenCalledWith({
      code: 'PROD-001',
      description: 'Produto de teste',
      stockBalance: 10,
      unit: 'UN'
    });
  });

  it('populates the form when editing an existing product', () => {
    const product: Product = {
      id: '11111111-1111-1111-1111-111111111111',
      code: 'PROD-002',
      description: 'Produto existente',
      stockBalance: 7.5,
      unit: 'KG',
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-22T10:00:00Z'
    };
    fixture.componentRef.setInput('product', product);

    fixture.detectChanges();

    expect(component.form.getRawValue()).toEqual({
      code: 'PROD-002',
      description: 'Produto existente',
      stockBalance: 7.5,
      unit: 'KG'
    });
  });
});
