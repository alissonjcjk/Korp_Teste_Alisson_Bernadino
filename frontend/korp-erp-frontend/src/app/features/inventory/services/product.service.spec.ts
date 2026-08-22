import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Product } from '../models/product.model';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProductService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(ProductService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads products and forwards the optional search term', () => {
    const product: Product = {
      id: '11111111-1111-1111-1111-111111111111',
      code: 'PROD-001',
      description: 'Produto de teste',
      stockBalance: 10,
      unit: 'UN',
      createdAt: '2026-08-20T10:00:00Z',
      updatedAt: '2026-08-22T10:00:00Z'
    };
    let result: Product[] | undefined;

    service.getAll('teste').subscribe(products => result = products);

    const request = http.expectOne(req =>
      req.url === '/api/inventory/products' && req.params.get('search') === 'teste'
    );
    expect(request.request.method).toBe('GET');
    request.flush({ success: true, message: 'ok', data: [product] });

    expect(result).toEqual([product]);
  });
});
