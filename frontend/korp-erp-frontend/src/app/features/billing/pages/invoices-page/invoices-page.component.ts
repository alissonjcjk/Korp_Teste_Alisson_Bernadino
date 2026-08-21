import {
  Component, ChangeDetectionStrategy, inject, signal, computed, OnInit
} from '@angular/core';
import { NgClass, DecimalPipe, DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../../services/invoice.service';
import { ToastService } from '../../../../core/services/toast.service';
import { InvoiceSummaryResponse, CreateInvoiceRequest } from '../../models/invoice.model';
import { InvoiceFormModalComponent } from '../../components/invoice-form-modal/invoice-form-modal.component';

@Component({
  selector: 'app-invoices-page',
  standalone: true,
  imports: [NgClass, DecimalPipe, DatePipe, CurrencyPipe, FormsModule, InvoiceFormModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="animate-in">

      <!-- Page Header -->
      <div class="page-header flex items-start justify-between">
        <div>
          <h1 class="page-title">
            <span class="text-gradient">Notas Fiscais</span>
          </h1>
          <p class="page-subtitle">Gerencie o faturamento e a impressão de NF</p>
        </div>
        <button (click)="openCreate()" id="btn-new-invoice" class="btn-primary">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
          </svg>
          Nova Nota Fiscal
        </button>
      </div>

      <!-- Stats Cards -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
        <div class="glass-card p-5">
          <p class="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-1">Total Faturado</p>
          <p class="text-3xl font-bold text-white">{{ totalRevenue() | currency:'BRL' }}</p>
        </div>
        <div class="glass-card p-5">
          <p class="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-1">Notas Abertas</p>
          <p class="text-3xl font-bold text-warning">{{ openInvoicesCount() }}</p>
        </div>
        <div class="glass-card p-5">
          <p class="text-xs font-semibold text-surface-400 uppercase tracking-wider mb-1">Notas Impressas</p>
          <p class="text-3xl font-bold text-success">{{ printedInvoicesCount() }}</p>
        </div>
      </div>

      <!-- Search + Table -->
      <div class="glass-card overflow-hidden">

        <!-- Search Bar -->
        <div class="p-5 border-b border-surface-800">
          <div class="relative max-w-sm">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-surface-500"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0"/>
            </svg>
            <input id="search-invoices" type="text" [(ngModel)]="searchTerm"
                   (ngModelChange)="onSearch($event)"
                   placeholder="Buscar por cliente ou nº..."
                   class="form-input pl-9"/>
          </div>
        </div>

        <!-- Loading -->
        @if (loading()) {
          <div class="flex items-center justify-center py-16 gap-3">
            <span class="spinner w-6 h-6"></span>
            <span class="text-surface-400 text-sm">Carregando notas fiscais...</span>
          </div>
        }

        <!-- Empty State -->
        @else if (filteredInvoices().length === 0) {
          <div class="flex flex-col items-center justify-center py-16 text-center gap-3">
            <div class="w-16 h-16 rounded-2xl bg-surface-800 flex items-center justify-center mb-2">
              <svg class="w-8 h-8 text-surface-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
              </svg>
            </div>
            <p class="text-surface-300 font-semibold">Nenhuma nota fiscal encontrada</p>
            <p class="text-surface-500 text-sm">
              {{ searchTerm ? 'Tente outro termo de busca.' : 'Clique em "Nova Nota Fiscal" para começar.' }}
            </p>
          </div>
        }

        <!-- Table -->
        @else {
          <div class="overflow-x-auto">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Nº NF</th>
                  <th>Cliente</th>
                  <th>Data</th>
                  <th>Status</th>
                  <th>Itens</th>
                  <th>Total</th>
                  <th class="text-right">Ações</th>
                </tr>
              </thead>
              <tbody>
                @for (invoice of filteredInvoices(); track invoice.id) {
                  <tr>
                    <td>
                      <span class="font-mono text-primary-300 font-semibold text-xs bg-primary-950/60
                                   px-2.5 py-1 rounded-lg border border-primary-800/40">
                        #{{ invoice.invoiceNumber.toString().padStart(6, '0') }}
                      </span>
                    </td>
                    <td class="font-medium">{{ invoice.customerName || 'Cliente não informado' }}</td>
                    <td class="text-surface-400 text-xs">{{ invoice.createdAt | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td>
                      <span class="badge" [ngClass]="{
                        'badge-open': invoice.status === 'Open',
                        'badge-printed': invoice.status === 'Closed',
                        'badge-cancelled': invoice.status === 'Cancelled'
                      }">
                        <span class="w-1.5 h-1.5 rounded-full" [ngClass]="{
                          'bg-warning': invoice.status === 'Open',
                          'bg-success': invoice.status === 'Closed',
                          'bg-danger': invoice.status === 'Cancelled'
                        }"></span>
                        {{ getStatusLabel(invoice.status) }}
                      </span>
                    </td>
                    <td class="text-surface-400 text-sm">{{ invoice.itemCount }} item(s)</td>
                    <td class="font-semibold text-primary-400">{{ invoice.totalAmount | currency:'BRL' }}</td>
                    <td>
                      <div class="flex items-center justify-end gap-2">
                        @if (invoice.status === 'Open') {
                          <button (click)="printInvoice(invoice)"
                                  [disabled]="printingId() === invoice.id"
                                  class="w-8 h-8 flex items-center justify-center rounded-lg text-surface-400
                                         hover:text-primary-400 hover:bg-primary-950/50 transition-all duration-150"
                                  title="Imprimir NF">
                            @if (printingId() === invoice.id) {
                              <span class="spinner w-4 h-4"></span>
                            } @else {
                              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
                              </svg>
                            }
                          </button>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="px-5 py-3 border-t border-surface-800 text-xs text-surface-500">
            {{ filteredInvoices().length }} nota(s) encontrada(s)
          </div>
        }
      </div>
    </div>

    <!-- Modal Create -->
    @if (showModal()) {
      <app-invoice-form-modal
        [loading]="saving()"
        (save)="onSave($event)"
        (cancel)="closeModal()"
      />
    }
  `
})
export class InvoicesPageComponent implements OnInit {
  private invoiceService = inject(InvoiceService);
  private toast = inject(ToastService);

  // State
  invoices   = signal<InvoiceSummaryResponse[]>([]);
  loading    = signal(false);
  saving     = signal(false);
  printingId = signal<string | null>(null);
  showModal  = signal(false);
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

  totalRevenue         = computed(() => this.invoices().filter(i => i.status !== 'Cancelled').reduce((acc, curr) => acc + curr.totalAmount, 0));
  openInvoicesCount    = computed(() => this.invoices().filter(i => i.status === 'Open').length);
  printedInvoicesCount = computed(() => this.invoices().filter(i => i.status === 'Closed').length);

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.loading.set(true);
    this.invoiceService.getAll().subscribe({
      next: invoices => { this.invoices.set(invoices); this.loading.set(false); },
      error: ()      => { this.loading.set(false); }
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
    switch(status) {
      case 'Open': return 'Aberta';
      case 'Closed': return 'Fechada';
      case 'Cancelled': return 'Cancelada';
      default: return status;
    }
  }
}
