import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output
} from '@angular/core';
import { NgClass } from '@angular/common';
import {
  AiRiskLevel,
  InvoiceAiAnalysisResponse
} from '../../models/ai-analysis.model';

@Component({
  selector: 'app-ai-analysis-modal',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ai-analysis-modal.component.html'
})
export class AiAnalysisModalComponent {
  @Input() invoiceNumber = 0;
  @Input() loading = false;
  @Input() analysis: InvoiceAiAnalysisResponse | null = null;
  @Output() close = new EventEmitter<void>();

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.close.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close.emit();
    }
  }

  riskLabel(riskLevel: AiRiskLevel): string {
    switch (riskLevel) {
      case 'low': return 'Risco baixo';
      case 'medium': return 'Risco médio';
      case 'high': return 'Risco alto';
      default: return 'Indisponível';
    }
  }
}
