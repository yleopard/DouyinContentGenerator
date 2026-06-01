import api from './api';
import type {
  CreateGenerationTaskRequest,
  GenerationTaskResponse,
  GeneratedImageResponse,
  GeneratedTextResponse,
  BudgetStatus,
  CostStats,
  GenerationStats,
} from '../types';

export const generationService = {
  createTask: (data: CreateGenerationTaskRequest) =>
    api.post<GenerationTaskResponse>('/generation-tasks', data),

  createBatchTasks: (tasks: CreateGenerationTaskRequest[]) =>
    api.post<GenerationTaskResponse[]>('/generation-tasks/batch', { tasks }),

  listTasks: (page = 1, pageSize = 20, status?: string) =>
    api.get<GenerationTaskResponse[]>('/generation-tasks', { params: { page, pageSize, status } }),

  getTask: (id: string) => api.get<GenerationTaskResponse>(`/generation-tasks/${id}`),

  cancelTask: (id: string) => api.post(`/generation-tasks/${id}/cancel`),

  getImages: (taskId: string, templateId?: string) =>
    api.get<GeneratedImageResponse[]>('/generated-contents/images', { params: { taskId, templateId } }),

  getTexts: (taskId: string, templateId?: string) =>
    api.get<GeneratedTextResponse[]>('/generated-contents/texts', { params: { taskId, templateId } }),

  selectContent: (taskId: string, selectedImageId?: string, selectedTextId?: string) =>
    api.post(`/generated-contents/${taskId}/select`, { selectedImageId, selectedTextId }),

  getBudgetStatus: () => api.get<BudgetStatus>('/statistics/budget-status'),

  getCostStats: (startDate?: string, endDate?: string) =>
    api.get<CostStats>('/statistics/cost', { params: { startDate, endDate } }),

  getGenerationStats: (period = 'month') =>
    api.get<GenerationStats>('/statistics/generation', { params: { period } }),
};
