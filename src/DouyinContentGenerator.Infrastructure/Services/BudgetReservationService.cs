using StackExchange.Redis;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.Services;

public class BudgetReservationService : IBudgetReservationService
{
    private readonly IDatabase _redis;
    private readonly decimal _dailyBudget;

    public BudgetReservationService(IConnectionMultiplexer redis, decimal dailyBudget = 200.0m)
    {
        _redis = redis.GetDatabase();
        _dailyBudget = dailyBudget;
    }

    public async Task<bool> TryReserve(Guid userId, Guid taskId, decimal amount)
    {
        var key = $"budget:{userId}:{DateTime.UtcNow:yyyyMMdd}";

        var used = (decimal)await _redis.StringGetAsync(key);

        if (used + amount > _dailyBudget)
            return false;

        var newValue = await _redis.StringIncrementAsync(key, (double)amount);

        if (newValue > (double)_dailyBudget)
        {
            await _redis.StringDecrementAsync(key, (double)amount);
            return false;
        }

        await _redis.HashSetAsync($"budget:reservations:{userId}", taskId.ToString(), amount.ToString());
        return true;
    }

    public async Task Release(Guid userId, decimal amount)
    {
        var key = $"budget:{userId}:{DateTime.UtcNow:yyyyMMdd}";
        var current = (decimal)await _redis.StringGetAsync(key);
        if (current >= amount)
            await _redis.StringDecrementAsync(key, (double)amount);
        else
            await _redis.KeyDeleteAsync(key);
    }

    public async Task<decimal> GetRemainingBudget(Guid userId)
    {
        var key = $"budget:{userId}:{DateTime.UtcNow:yyyyMMdd}";
        var used = (decimal)await _redis.StringGetAsync(key);
        return Math.Max(0, _dailyBudget - used);
    }

    public async Task<int> GetEstimatedRemainingTasks(Guid userId)
    {
        var remaining = await GetRemainingBudget(userId);
        const decimal avgCostPerTask = 2.5m;
        return (int)(remaining / avgCostPerTask);
    }
}
