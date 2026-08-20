import { Injectable } from '@angular/core';
import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'Ocorreu um erro inesperado.';

      if (error.status === 0 || error.status === 503) {
        // Serviço indisponível — demonstra o Circuit Breaker visualmente
        message = 'Serviço temporariamente indisponível. Tente novamente em instantes.';
        toast.error(message, 'Serviço Indisponível');
      } else if (error.status === 400) {
        message = error.error?.detail || error.error?.title || 'Requisição inválida.';
        toast.warning(message, 'Atenção');
      } else if (error.status === 404) {
        message = error.error?.detail || 'Recurso não encontrado.';
        toast.warning(message, 'Não encontrado');
      } else if (error.status === 409) {
        message = error.error?.detail || 'Conflito de dados.';
        toast.error(message, 'Conflito');
      } else if (error.status >= 500) {
        message = 'Erro interno no servidor.';
        toast.error(message, 'Erro do Servidor');
      }

      return throwError(() => ({
        status: error.status,
        message,
        originalError: error
      }));
    })
  );
};
