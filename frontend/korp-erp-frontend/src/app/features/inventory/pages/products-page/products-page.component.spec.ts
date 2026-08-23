import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ToastService } from '../../../../core/services/toast.service';
import { Product } from '../../models/product.model';
import { ProductService } from '../../services/product.service';
import { ProductsPageComponent } from './products-page.component';

describe('ProductsPageComponent', () => {
  let productService: jasmine.SpyObj<ProductService>;
  let toast: jasmine.SpyObj<ToastService>;
  let component: ProductsPageComponent;

  const product: Product = {
    id: 'product-1',
    code: 'PROD-001',
    description: 'Produto original',
    stockBalance: 12.5,
    unit: 'UN',
    createdAt: '2026-08-22T00:00:00Z',
    updatedAt: '2026-08-22T00:00:00Z'
  };

  beforeEach(() => {
    productService = jasmine.createSpyObj<ProductService>(
      'ProductService',
      ['getAll', 'create', 'update', 'delete']
    );
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['success']);
    productService.getAll.and.returnValue(of([]));
    productService.update.and.returnValue(of(product));

    TestBed.configureTestingModule({
      providers: [
        { provide: ProductService, useValue: productService },
        { provide: ToastService, useValue: toast }
      ]
    });

    component = TestBed.runInInjectionContext(() => new ProductsPageComponent());
  });

  it('sends only mutable fields when updating a product', () => {
    component.selectedProduct.set(product);

    component.onSave({
      code: 'PROD-ALTERADO',
      description: 'Produto atualizado',
      stockBalance: 999,
      unit: 'KG'
    });

    expect(productService.update).toHaveBeenCalledOnceWith('product-1', {
      description: 'Produto atualizado',
      unit: 'KG'
    });
  });
});
