import { inject } from '@angular/core';
import { HttpErrorResponse, HttpHandlerFn, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiFieldErrors, NormalizedHttpError } from '../models/api-response.model';
import { ToastService } from '../services/toast.service';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readFieldErrors(body: unknown): ApiFieldErrors {
  if (!isRecord(body) || !isRecord(body['errors'])) {
    return {};
  }

  return Object.entries(body['errors']).reduce<ApiFieldErrors>((result, [field, messages]) => {
    if (!Array.isArray(messages)) {
      return result;
    }

    const validMessages = messages
      .filter((message): message is string => typeof message === 'string')
      .map(message => message.trim())
      .filter(message => message.length > 0);

    if (validMessages.length > 0) {
      result[field] = validMessages;
    }

    return result;
  }, {});
}

function readText(body: unknown, property: string): string | undefined {
  if (!isRecord(body) || typeof body[property] !== 'string') {
    return undefined;
  }

  const value = body[property].trim();
  return value.length > 0 ? value : undefined;
}

function fallbackMessage(status: number): string {
  switch (status) {
    case 0:
      return 'Não foi possível se comunicar com o servidor. Verifique sua conexão.';
    case 400:
      return 'Requisição inválida.';
    case 404:
      return 'Recurso não encontrado.';
    case 409:
      return 'A operação entrou em conflito com o estado atual dos dados.';
    case 503:
      return 'Serviço temporariamente indisponível. Tente novamente em instantes.';
    default:
      return status >= 500
        ? 'Ocorreu um erro interno no servidor.'
        : 'Ocorreu um erro inesperado.';
  }
}

function showToast(toast: ToastService, status: number, message: string): void {
  if (status === 400) {
    toast.warning(message, 'Dados inválidos');
  } else if (status === 404) {
    toast.warning(message, 'Não encontrado');
  } else if (status === 409) {
    toast.error(message, 'Conflito');
  } else if (status === 0 || status === 503) {
    toast.error(message, 'Serviço indisponível');
  } else if (status >= 500) {
    toast.error(message, 'Erro do servidor');
  } else {
    toast.error(message);
  }
}

export const errorInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn
) => {
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const fieldErrors = readFieldErrors(error.error);
      const validationMessages = [...new Set(Object.values(fieldErrors).flat())];
      const backendMessage = readText(error.error, 'message');
      const message = validationMessages.length > 0
        ? validationMessages.join(' ')
        : backendMessage ?? fallbackMessage(error.status);

      showToast(toast, error.status, message);

      const normalizedError: NormalizedHttpError = {
        status: error.status,
        message,
        fieldErrors,
        traceId: readText(error.error, 'traceId'),
        originalError: error
      };

      return throwError(() => normalizedError);
    })
  );
};
