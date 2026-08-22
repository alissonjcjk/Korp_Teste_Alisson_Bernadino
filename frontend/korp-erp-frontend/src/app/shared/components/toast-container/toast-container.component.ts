import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { NgClass } from '@angular/common';
import { ToastService, Toast } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [NgClass],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toast-container.component.html'
})
export class ToastContainerComponent {
  readonly toastService = inject(ToastService);

  getClass(toast: Toast): Record<string, boolean> {
    return {
      'bg-surface-900 border-success/40': toast.type === 'success',
      'bg-surface-900 border-danger/40':  toast.type === 'error',
      'bg-surface-900 border-warning/40': toast.type === 'warning',
      'bg-surface-900 border-primary-500/40': toast.type === 'info',
    };
  }
}
