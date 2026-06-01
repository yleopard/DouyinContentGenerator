import api from './api';
import type { ImageTemplate, CopywritingTemplate } from '../types';

export const templateService = {
  getImageTemplates: (category?: string) =>
    api.get<ImageTemplate[]>('/image-templates', { params: { category } }),

  getImageTemplate: (id: string) => api.get<ImageTemplate>(`/image-templates/${id}`),

  createImageTemplate: (data: Partial<ImageTemplate>) =>
    api.post<ImageTemplate>('/image-templates', data),

  updateImageTemplate: (id: string, data: Partial<ImageTemplate>) =>
    api.put<ImageTemplate>(`/image-templates/${id}`, data),

  deleteImageTemplate: (id: string) => api.delete(`/image-templates/${id}`),

  getCopywritingTemplates: (type?: string) =>
    api.get<CopywritingTemplate[]>('/copywriting-templates', { params: { type } }),

  createCopywritingTemplate: (data: Partial<CopywritingTemplate>) =>
    api.post<CopywritingTemplate>('/copywriting-templates', data),

  updateCopywritingTemplate: (id: string, data: Partial<CopywritingTemplate>) =>
    api.put<CopywritingTemplate>(`/copywriting-templates/${id}`, data),

  deleteCopywritingTemplate: (id: string) => api.delete(`/copywriting-templates/${id}`),
};
