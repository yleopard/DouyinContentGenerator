using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IBudgetReservationService _budgetService;

    public HealthController(ApplicationDbContext db, IBudgetReservationService budgetService)
    {
        _db = db;
        _budgetService = budgetService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbOk = false;
        var redisOk = false;

        try
        {
            await _db.Database.CanConnectAsync();
            dbOk = true;
        }
        catch { }

        try
        {
            var testBudget = await _budgetService.GetRemainingBudget(Guid.Empty);
            redisOk = true;
        }
        catch { }

        return Ok(new
        {
            status = dbOk && redisOk ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            services = new { database = dbOk, redis = redisOk }
        });
    }
}
