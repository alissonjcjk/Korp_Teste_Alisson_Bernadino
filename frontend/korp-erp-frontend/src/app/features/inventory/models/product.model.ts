// Representa a resposta da API envolvida no ApiResponse<T>
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

export interface Product {
  id: string;
  code: string;
  description: string;
  stockBalance: number;
  unit: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  code: string;
  description: string;
  stockBalance: number;
  unit: string;
}

export interface UpdateProductRequest {
  code: string;
  description: string;
  stockBalance: number;
  unit: string;
}
