import { useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { generationService } from '../services/generationService';
import type { TaskProgressUpdate } from '../types';

export const useGenerationTask = () => {
  const queryClient = useQueryClient();

  const tasksQuery = useQuery({
    queryKey: ['generation-tasks'],
    queryFn: () => generationService.listTasks().then(r => r.data),
  });

  const createTask = useMutation({
    mutationFn: generationService.createTask,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['generation-tasks'] }),
  });

  const cancelTask = useMutation({
    mutationFn: generationService.cancelTask,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['generation-tasks'] }),
  });

  return {
    tasks: tasksQuery.data ?? [],
    isLoading: tasksQuery.isLoading,
    createTask: useCallback(
      (data: Parameters<typeof generationService.createTask>[0]) => createTask.mutateAsync(data),
      [createTask]
    ),
    cancelTask: useCallback(
      (id: string) => cancelTask.mutate(id),
      [cancelTask]
    ),
  };
};
