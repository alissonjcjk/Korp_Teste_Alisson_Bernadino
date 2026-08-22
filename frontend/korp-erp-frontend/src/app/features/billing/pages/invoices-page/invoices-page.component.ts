import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit
} from '@angular/core';
import { NgClass, DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../../services/invoice.service';
import { ToastService } from '../../../../core/services/toast.service';
import { InvoiceSummaryResponse, CreateInvoiceRequest } from '../../models/invoice.model';
import { InvoiceFormModalComponent } from '../../components/invoice-form-modal/invoice-form-modal.component';

@Component({
  selector: 'app-invoices-page',
  standalone: true,
  imports: [NgClass, DatePipe, CurrencyPipe, FormsModule, InvoiceFormModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './invoices-page.component.html'
})
export class InvoicesPageComponent implements OnInit {
  private invoiceService = inject(InvoiceService);
  private toast = inject(ToastService);

  // State
  invoices = signal<InvoiceSummaryResponse[]>([]);
  loading = signal(false);
  saving = signal(false);
  printingId = signal<string | null>(null);
  showModal = signal(false);
  searchTerm = '';

  // Computed
  filteredInvoices = computed(() => {
    const term = this.searchTerm.toLowerCase();
    if (!term) return this.invoices();
    return this.invoices().filter(i =>
      i.invoiceNumber.toString().includes(term) ||
      (i.customerName && i.customerName.toLowerCase().includes(term))
    );
  });

  totalRevenue = computed(() => this.invoices().filter(i => i.status !== 'Cancelled').reduce((acc, curr) => acc + curr.totalAmount, 0));
  openInvoicesCount = computed(() => this.invoices().filter(i => i.status === 'Open').length);
  printedInvoicesCount = computed(() => this.invoices().filter(i => i.status === 'Closed').length);

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.loading.set(true);
    this.invoiceService.getAll().subscribe({
      next: invoices => { this.invoices.set(invoices); this.loading.set(false); },
      error: () => { this.loading.set(false); }
    });
  }

  onSearch(term: string): void {
    this.searchTerm = term;
  }

  openCreate(): void {
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
  }

  onSave(request: CreateInvoiceRequest): void {
    this.saving.set(true);
    this.invoiceService.create(request).subscribe({
      next: () => {
        this.toast.success('Nota fiscal criada com sucesso!', 'Cadastrado');
        this.saving.set(false);
        this.closeModal();
        this.loadInvoices();
      },
      error: () => { this.saving.set(false); }
    });
  }

  printInvoice(invoice: InvoiceSummaryResponse): void {
    // Generate idempotency key
    const idempotencyKey = crypto.randomUUID();
    this.printingId.set(invoice.id);

    this.invoiceService.print(invoice.id, idempotencyKey).subscribe({
      next: () => {
        this.toast.success(`NF #${invoice.invoiceNumber} impressa com sucesso!`, 'Impresso');
        this.printingId.set(null);
        this.loadInvoices(); // Refresh list to update status and badges
      },
      error: () => { this.printingId.set(null); }
    });
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Open': return 'Aberta';
      case 'Closed': return 'Fechada';
      case 'Cancelled': return 'Cancelada';
      default: return status;
    }
  }
}
