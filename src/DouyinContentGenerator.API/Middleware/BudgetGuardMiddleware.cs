using System.Security.Claims;
using System.Text.Json;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.API.Middleware;

public class BudgetGuardMiddleware
{
    private readonly RequestDelegate _next;

    public BudgetGuardMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IBudgetReservationService budgetService)
    {
        if (context.Request.Path.StartsWithSegments("/api/generation-tasks") &&
            context.Request.Method == "POST")
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var remaining = await budgetService.GetRemainingBudget(userId);
                if (remaining <= 0)
                {
                    context.Response.StatusCode = 429;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        error = "Daily budget exceeded",
                        message = "今日预算已用完,请明天再试或联系管理员增加预算"
                    }));
                    return;
                }
            }
        }

        await _next(context);
    }
}
