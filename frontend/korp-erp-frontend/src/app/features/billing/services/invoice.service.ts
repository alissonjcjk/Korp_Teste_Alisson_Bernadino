import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  InvoiceSummaryResponse,
  InvoiceResponse,
  CreateInvoiceRequest
} from '../models/invoice.model';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/billing/invoices';

  getAll(): Observable<InvoiceSummaryResponse[]> {
    return this.http
      .get<ApiResponse<InvoiceSummaryResponse[]>>(this.baseUrl)
      .pipe(map(r => r.data));
  }

  getById(id: string): Observable<InvoiceResponse> {
    return this.http
      .get<ApiResponse<InvoiceResponse>>(`${this.baseUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  create(request: CreateInvoiceRequest): Observable<InvoiceResponse> {
    return this.http
      .post<ApiResponse<InvoiceResponse>>(this.baseUrl, request)
      .pipe(map(r => r.data));
  }

  print(id: string, idempotencyKey: string): Observable<InvoiceResponse> {
    return this.http
      .post<ApiResponse<InvoiceResponse>>(`${this.baseUrl}/${id}/print`, null, {
        headers: { 'Idempotency-Key': idempotencyKey }
      })
      .pipe(map(r => r.data));
  }
}
