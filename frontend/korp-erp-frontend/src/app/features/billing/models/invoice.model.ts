export interface InvoiceItemResponse {
  id: string;
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface InvoiceResponse {
  id: string;
  invoiceNumber: number;
  status: 'Open' | 'Closed' | 'Cancelled';
  customerName?: string;
  notes?: string;
  totalAmount: number;
  printedAt?: string;
  createdAt: string;
  updatedAt: string;
  items: InvoiceItemResponse[];
}

export interface InvoiceSummaryResponse {
  id: string;
  invoiceNumber: number;
  status: 'Open' | 'Closed' | 'Cancelled';
  customerName?: string;
  totalAmount: number;
  itemCount: number;
  createdAt: string;
}

export interface CreateInvoiceItemRequest {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateInvoiceRequest {
  customerName?: string;
  notes?: string;
  items: CreateInvoiceItemRequest[];
}
