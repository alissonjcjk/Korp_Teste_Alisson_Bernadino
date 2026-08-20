import { Injectable, signal, computed } from '@angular/core';

export interface Toast {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = signal<Toast[]>([]);
  readonly toasts = computed(() => this._toasts());

  success(message: string, title = 'Sucesso', duration = 4000): void {
    this.add({ type: 'success', title, message, duration });
  }

  error(message: string, title = 'Erro', duration = 6000): void {
    this.add({ type: 'error', title, message, duration });
  }

  warning(message: string, title = 'Atenção', duration = 5000): void {
    this.add({ type: 'warning', title, message, duration });
  }

  info(message: string, title = 'Info', duration = 4000): void {
    this.add({ type: 'info', title, message, duration });
  }

  remove(id: string): void {
    this._toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  private add(toast: Omit<Toast, 'id'>): void {
    const id = crypto.randomUUID();
    this._toasts.update(toasts => [...toasts, { ...toast, id }]);
    setTimeout(() => this.remove(id), toast.duration);
  }
}
