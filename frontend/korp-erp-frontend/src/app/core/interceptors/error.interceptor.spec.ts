import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { NormalizedHttpError } from '../models/api-response.model';
import { ToastService } from '../services/toast.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(() => {
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['error', 'warning']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast }
      ]
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('uses field validation messages from a standardized 400 response', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.flush({
      success: false,
      statusCode: 400,
      message: 'Um ou mais campos são inválidos.',
      errors: {
        stockBalance: ['O saldo inicial é obrigatório.'],
        unit: ['A unidade é obrigatória.']
      },
      traceId: 'trace-validation',
      timestamp: '2026-08-22T12:00:00Z'
    }, { status: 400, statusText: 'Bad Request' });

    const error = await errorPromise;

    expect(error.message).toBe('O saldo inicial é obrigatório. A unidade é obrigatória.');
    expect(error.fieldErrors['stockBalance']).toEqual(['O saldo inicial é obrigatório.']);
    expect(error.traceId).toBe('trace-validation');
    expect(toast.warning).toHaveBeenCalledWith(error.message, 'Dados inválidos');
  });

  it('preserves the backend business message for 404', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.flush(apiError(404, 'Produto não encontrado.'), {
      status: 404,
      statusText: 'Not Found'
    });

    const error = await errorPromise;

    expect(error.message).toBe('Produto não encontrado.');
    expect(toast.warning).toHaveBeenCalledWith('Produto não encontrado.', 'Não encontrado');
  });

  it('preserves the backend business message for 409', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.flush(apiError(409, 'Já existe um produto com este código.'), {
      status: 409,
      statusText: 'Conflict'
    });

    const error = await errorPromise;

    expect(error.message).toBe('Já existe um produto com este código.');
    expect(toast.error).toHaveBeenCalledWith(error.message, 'Conflito');
  });

  it('shows the safe 503 message returned by the backend', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.flush(apiError(503, 'O Serviço de Estoque está indisponível no momento.'), {
      status: 503,
      statusText: 'Service Unavailable'
    });

    const error = await errorPromise;

    expect(error.message).toBe('O Serviço de Estoque está indisponível no momento.');
    expect(toast.error).toHaveBeenCalledWith(error.message, 'Serviço indisponível');
  });

  it('uses a connection fallback when there is no HTTP response', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.error(new ProgressEvent('network error'));

    const error = await errorPromise;

    expect(error.status).toBe(0);
    expect(error.message).toBe('Não foi possível se comunicar com o servidor. Verifique sua conexão.');
    expect(toast.error).toHaveBeenCalledWith(error.message, 'Serviço indisponível');
  });

  it('preserves the safe message from a standardized 500 response', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.flush(apiError(500, 'Ocorreu um erro interno no servidor.'), {
      status: 500,
      statusText: 'Internal Server Error'
    });

    const error = await errorPromise;

    expect(error.message).toBe('Ocorreu um erro interno no servidor.');
    expect(error.traceId).toBe('trace-500');
    expect(toast.error).toHaveBeenCalledWith(error.message, 'Erro do servidor');
  });

  it('never uses legacy detail or stack trace fields as user feedback', async () => {
    const errorPromise = captureError();
    const request = controller.expectOne('/test');

    request.flush({ detail: 'System.Exception: segredo interno' }, {
      status: 500,
      statusText: 'Internal Server Error'
    });

    const error = await errorPromise;

    expect(error.message).toBe('Ocorreu um erro interno no servidor.');
    expect(error.message).not.toContain('segredo interno');
    expect(toast.error).toHaveBeenCalledWith(error.message, 'Erro do servidor');
  });

  function captureError(): Promise<NormalizedHttpError> {
    return firstValueFrom(http.get('/test'))
      .then(() => Promise.reject(new Error('A requisição deveria falhar.')))
      .catch((error: NormalizedHttpError) => error);
  }

  function apiError(statusCode: number, message: string) {
    return {
      success: false,
      statusCode,
      message,
      traceId: `trace-${statusCode}`,
      timestamp: '2026-08-22T12:00:00Z'
    };
  }
});
