using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StatisticsController(ApplicationDbContext db)
    {
        _db = db;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim?.Value ?? throw new UnauthorizedAccessException());
    }

    [HttpGet("cost")]
    public async Task<ActionResult> GetCostStats(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var userId = GetCurrentUserId();
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var imageCosts = await _db.GeneratedImages
            .Where(i => i.Task.UserId == userId && i.CreatedAt >= start && i.CreatedAt <= end)
            .SumAsync(i => i.Cost);

        var textCosts = await _db.GeneratedTexts
            .Where(t => t.Task.UserId == userId && t.CreatedAt >= start && t.CreatedAt <= end)
            .SumAsync(t => t.Cost);

        var imageCount = await _db.GeneratedImages
            .CountAsync(i => i.Task.UserId == userId && i.CreatedAt >= start && i.CreatedAt <= end);

        var textCount = await _db.GeneratedTexts
            .CountAsync(t => t.Task.UserId == userId && t.CreatedAt >= start && t.CreatedAt <= end);

        return Ok(new
        {
            startDate = start,
            endDate = end,
            imageCost = imageCosts,
            textCost = textCosts,
            totalCost = imageCosts + textCosts,
            imageCount,
            textCount
        });
    }

    [HttpGet("generation")]
    public async Task<ActionResult> GetGenerationStats([FromQuery] string period = "month")
    {
        var userId = GetCurrentUserId();
        var days = period == "week" ? 7 : period == "month" ? 30 : 365;

        var since = DateTime.UtcNow.AddDays(-days);

        var total = await _db.GenerationTasks
            .CountAsync(t => t.UserId == userId && t.CreatedAt >= since);

        var completed = await _db.GenerationTasks
            .CountAsync(t => t.UserId == userId && t.CreatedAt >= since && t.Status == "completed");

        var failed = await _db.GenerationTasks
            .CountAsync(t => t.UserId == userId && t.CreatedAt >= since && t.Status == "failed");

        return Ok(new { total, completed, failed, period, since });
    }

    [HttpGet("budget-status")]
    public async Task<ActionResult> GetBudgetStatus([FromServices] Core.Interfaces.IBudgetReservationService budgetService)
    {
        var userId = GetCurrentUserId();
        var remaining = await budgetService.GetRemainingBudget(userId);
        var estimatedTasks = await budgetService.GetEstimatedRemainingTasks(userId);

        return Ok(new
        {
            dailyBudget = 200.0m,
            remainingBudget = remaining,
            usedBudget = 200.0m - remaining,
            estimatedRemainingTasks = estimatedTasks
        });
    }
}
