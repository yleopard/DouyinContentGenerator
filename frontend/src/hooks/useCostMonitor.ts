import { useQuery } from '@tanstack/react-query';
import { generationService } from '../services/generationService';

export const useCostMonitor = (startDate?: string, endDate?: string) => {
  const budgetQuery = useQuery({
    queryKey: ['budget-status'],
    queryFn: () => generationService.getBudgetStatus().then(r => r.data),
    refetchInterval: 30000,
  });

  const costQuery = useQuery({
    queryKey: ['cost-stats', startDate, endDate],
    queryFn: () => generationService.getCostStats(startDate, endDate).then(r => r.data),
  });

  const statsQuery = useQuery({
    queryKey: ['generation-stats'],
    queryFn: () => generationService.getGenerationStats().then(r => r.data),
  });

  return {
    budget: budgetQuery.data,
    costStats: costQuery.data,
    genStats: statsQuery.data,
    isLoading: budgetQuery.isLoading || costQuery.isLoading,
  };
};
