import api from './api';
import type { CreateProductRequest, UpdateProductRequest, ProductResponse } from '../types';

export const productService = {
  list: (page = 1, pageSize = 20, category?: string) =>
    api.get<ProductResponse[]>('/products', { params: { page, pageSize, category } }),

  get: (id: string) => api.get<ProductResponse>(`/products/${id}`),

  create: (data: CreateProductRequest) => api.post<ProductResponse>('/products', data),

  update: (id: string, data: UpdateProductRequest) =>
    api.put<ProductResponse>(`/products/${id}`, data),

  delete: (id: string) => api.delete(`/products/${id}`),

  uploadImage: (productId: string, file: File, type = 'product', order = 0) => {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('type', type);
    formData.append('order', String(order));
    return api.post(`/products/${productId}/images`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  deleteImage: (productId: string, imageId: string) =>
    api.delete(`/products/${productId}/images/${imageId}`),
};
