import { HttpErrorResponse } from '@angular/common/http';

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export type ApiFieldErrors = Record<string, string[]>;

export interface ApiErrorResponse {
  success: false;
  statusCode: number;
  message: string;
  errors?: ApiFieldErrors;
  traceId: string;
  timestamp: string;
}

export interface NormalizedHttpError {
  status: number;
  message: string;
  fieldErrors: ApiFieldErrors;
  traceId?: string;
  originalError: HttpErrorResponse;
}
