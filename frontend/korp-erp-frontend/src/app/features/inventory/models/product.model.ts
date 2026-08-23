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
  description: string;
  unit: string;
}
