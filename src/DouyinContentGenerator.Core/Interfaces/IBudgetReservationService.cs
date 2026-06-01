using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface IBudgetReservationService
{
    Task<bool> TryReserve(Guid userId, Guid taskId, decimal amount);
    Task Release(Guid userId, decimal amount);
    Task<decimal> GetRemainingBudget(Guid userId);
    Task<int> GetEstimatedRemainingTasks(Guid userId);
}
