import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InvoiceResponse } from '../models/invoice.model';
import { InvoiceService } from './invoice.service';

describe('InvoiceService', () => {
  let service: InvoiceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [InvoiceService, provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(InvoiceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the idempotency key when printing an invoice', () => {
    const invoice: InvoiceResponse = {
      id: 'aaaaaaaa-1111-1111-1111-111111111111',
      invoiceNumber: 1,
      status: 'Closed',
      totalAmount: 30,
      printedAt: '2026-08-22T12:00:00Z',
      createdAt: '2026-08-22T10:00:00Z',
      updatedAt: '2026-08-22T12:00:00Z',
      items: []
    };
    let result: InvoiceResponse | undefined;

    service.print(invoice.id, 'print-operation-1').subscribe(value => result = value);

    const request = http.expectOne(`/api/billing/invoices/${invoice.id}/print`);
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Idempotency-Key')).toBe('print-operation-1');
    request.flush({ success: true, message: 'ok', data: invoice });

    expect(result).toEqual(invoice);
  });
});
