import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import {
  ApiResponse,
  Product,
  CreateProductRequest,
  UpdateProductRequest
} from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/inventory/products';

  getAll(search = ''): Observable<Product[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http
      .get<ApiResponse<Product[]>>(this.baseUrl, { params })
      .pipe(map(r => r.data));
  }

  getById(id: string): Observable<Product> {
    return this.http
      .get<ApiResponse<Product>>(`${this.baseUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  create(request: CreateProductRequest): Observable<Product> {
    return this.http
      .post<ApiResponse<Product>>(this.baseUrl, request)
      .pipe(map(r => r.data));
  }

  update(id: string, request: UpdateProductRequest): Observable<Product> {
    return this.http
      .put<ApiResponse<Product>>(`${this.baseUrl}/${id}`, request)
      .pipe(map(r => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
