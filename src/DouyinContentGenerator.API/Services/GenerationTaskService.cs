using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.API.Hubs;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.API.Services;

public class GenerationTaskService : IGenerationTaskService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<GenerationHub> _hubContext;
    private readonly ICostCalculator _costCalculator;
    private readonly IBudgetReservationService _budgetService;
    private readonly IImageGenerator _imageGenerator;
    private readonly ITextGenerator _textGenerator;
    private readonly ILogger<GenerationTaskService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public GenerationTaskService(
        ApplicationDbContext db,
        IHubContext<GenerationHub> hubContext,
        ICostCalculator costCalculator,
        IBudgetReservationService budgetService,
        IImageGenerator imageGenerator,
        ITextGenerator textGenerator,
        ILogger<GenerationTaskService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _hubContext = hubContext;
        _costCalculator = costCalculator;
        _budgetService = budgetService;
        _imageGenerator = imageGenerator;
        _textGenerator = textGenerator;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<GenerationTaskResponse> CreateTaskAsync(Guid userId, CreateGenerationTaskRequest request)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.UserId == userId)
            ?? throw new InvalidOperationException("Product not found");

        var imgTemplates = await _db.ImageTemplates
            .Where(t => request.ImageTemplateIds.Contains(t.Id))
            .ToListAsync();

        var txtTemplates = await _db.CopywritingTemplates
            .Where(t => request.TextTemplateIds.Contains(t.Id))
            .ToListAsync();

        var (estImgCost, estTxtCost) = _costCalculator.EstimateTaskCost(
            request.ImageCount * imgTemplates.Count,
            request.TextVariantsCount * txtTemplates.Count,
            "tongyi_wanxiang", "tongyi_qianwen",
            request.UseReferenceImage);

        var totalEstCost = estImgCost + estTxtCost;

        var task = new GenerationTask
        {
            UserId = userId,
            ProductId = request.ProductId,
            Status = "pending",
            ImageCount = request.ImageCount,
            TextVariantsCount = request.TextVariantsCount,
            UseReferenceImage = request.UseReferenceImage,
            EstimatedCost = totalEstCost
        };

        if (!await _budgetService.TryReserve(userId, task.Id, totalEstCost))
            throw new InvalidOperationException("Daily budget exceeded");

        _db.GenerationTasks.Add(task);

        foreach (var tplId in request.ImageTemplateIds)
            _db.TaskImageTemplates.Add(new TaskImageTemplate { TaskId = task.Id, ImageTemplateId = tplId });

        await _db.SaveChangesAsync();

        // Background processing in NEW scope
        var taskId = task.Id;
        var pId = product.Id;
        var imgTplIds = imgTemplates.Select(t => t.Id).ToList();
        var txtTplIds = txtTemplates.Select(t => t.Id).ToList();
        var useRef = request.UseReferenceImage;

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<GenerationHub>>();
            var budget = scope.ServiceProvider.GetRequiredService<IBudgetReservationService>();
            var genFactory = scope.ServiceProvider.GetRequiredService<IUserAIGeneratorFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<GenerationTaskService>>();

            // Create generators with user's API keys from database
            var (imgGen, _, _) = await genFactory.CreateImageGeneratorAsync(userId);
            var (txtGen, _, _) = await genFactory.CreateTextGeneratorAsync(userId);

            await ProcessTaskAsync(db, hub, budget, imgGen, txtGen, logger,
                taskId, imgTplIds, txtTplIds, pId, useRef);
        });

        return MapToResponse(task);
    }

    public async Task<GenerationTaskResponse?> GetTaskAsync(Guid userId, Guid taskId)
    {
        var task = await _db.GenerationTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        return task != null ? MapToResponse(task) : null;
    }

    public async Task<List<GenerationTaskResponse>> GetTasksAsync(Guid userId, int page = 1, int pageSize = 20, string? status = null)
    {
        var query = _db.GenerationTasks.Where(t => t.UserId == userId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        var tasks = await query.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return tasks.Select(MapToResponse).ToList();
    }

    public async Task<bool> CancelTaskAsync(Guid userId, Guid taskId)
    {
        var task = await _db.GenerationTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task == null || task.Status is "completed" or "failed" or "cancelled") return false;
        task.Status = "cancelled"; task.CompletedAt = DateTime.UtcNow;
        var actualCost = await _db.GeneratedImages.Where(i => i.TaskId == taskId && i.Status == "success").SumAsync(i => i.Cost)
            + await _db.GeneratedTexts.Where(t => t.TaskId == taskId && t.Status == "success").SumAsync(t => t.Cost);
        task.ActualCost = actualCost;
        await _db.SaveChangesAsync();
        await _budgetService.Release(userId, task.EstimatedCost - actualCost);
        await _hubContext.Clients.Group($"task_{taskId}").SendAsync("TaskProgressUpdated", new { taskId, status = "cancelled", progress = 0 });
        return true;
    }

    public async Task<List<GeneratedImageResponse>> GetGeneratedImagesAsync(Guid taskId, Guid? templateId = null)
    {
        var query = _db.GeneratedImages.Where(i => i.TaskId == taskId);
        if (templateId.HasValue) query = query.Where(i => i.ImageTemplateId == templateId.Value);
        return await query.Select(i => new GeneratedImageResponse
        {
            Id = i.Id, TaskId = i.TaskId, ImageTemplateId = i.ImageTemplateId,
            ImageUrl = i.ImageUrl, Status = i.Status, IsSelected = i.IsSelected, CreatedAt = i.CreatedAt
        }).ToListAsync();
    }

    public async Task<List<GeneratedTextResponse>> GetGeneratedTextsAsync(Guid taskId, Guid? templateId = null)
    {
        var query = _db.GeneratedTexts.Where(t => t.TaskId == taskId);
        if (templateId.HasValue) query = query.Where(t => t.CopywritingTemplateId == templateId.Value);
        return await query.Select(t => new GeneratedTextResponse
        {
            Id = t.Id, TaskId = t.TaskId, CopywritingTemplateId = t.CopywritingTemplateId,
            Content = t.Content, Status = t.Status, IsSelected = t.IsSelected, CreatedAt = t.CreatedAt
        }).ToListAsync();
    }

    public async Task<bool> SelectContentAsync(Guid taskId, SelectContentRequest request)
    {
        if (request.SelectedImageId.HasValue)
        {
            await _db.GeneratedImages.Where(i => i.TaskId == taskId && i.Id != request.SelectedImageId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsSelected, false));
            await _db.GeneratedImages.Where(i => i.Id == request.SelectedImageId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.IsSelected, true));
        }
        if (request.SelectedTextId.HasValue)
        {
            await _db.GeneratedTexts.Where(t => t.TaskId == taskId && t.Id != request.SelectedTextId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsSelected, false));
            await _db.GeneratedTexts.Where(t => t.Id == request.SelectedTextId.Value)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsSelected, true));
        }
        return true;
    }

    // === Background Processing (with fresh scope) ===

    private static async Task ProcessTaskAsync(
        ApplicationDbContext db, IHubContext<GenerationHub> hubContext, IBudgetReservationService budgetService,
        IImageGenerator imageGenerator, ITextGenerator textGenerator, ILogger<GenerationTaskService> logger,
        Guid taskId, List<Guid> imgTplIds, List<Guid> txtTplIds, Guid productId, bool useRef)
    {
        var task = await db.GenerationTasks.FindAsync(taskId);
        if (task == null) return;

        task.Status = "processing";
        task.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var product = await db.Products.FindAsync(productId);
        if (product == null) return;

        var imgTemplates = await db.ImageTemplates.Where(t => imgTplIds.Contains(t.Id)).ToListAsync();
        var txtTemplates = await db.CopywritingTemplates.Where(t => txtTplIds.Contains(t.Id)).ToListAsync();

        var totalSteps = imgTplIds.Count * task.ImageCount + txtTplIds.Count * task.TextVariantsCount;

        await UpdateProgressAsync(db, hubContext, taskId, 0, "开始生成...");

        // Launch all sub-jobs in parallel
        var allTasks = new List<Task>();
        foreach (var tpl in imgTemplates)
            for (int i = 0; i < task.ImageCount; i++)
                allTasks.Add(GenerateImageAsync(db, hubContext, imageGenerator, logger, taskId, tpl, product, useRef));

        foreach (var tpl in txtTemplates)
            for (int i = 0; i < task.TextVariantsCount; i++)
                allTasks.Add(GenerateTextAsync(db, hubContext, textGenerator, logger, taskId, tpl, product));

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(15));

        while (allTasks.Any(t => !t.IsCompleted))
        {
            if (cts.Token.IsCancellationRequested)
            {
                // Timeout: mark remaining tasks as failed
                foreach (var t in allTasks.Where(t => !t.IsCompleted))
                    await PushItemStatus(hubContext, taskId, "unknown", "unknown", "failed", "任务超时");
                break;
            }
            var successImages = await db.GeneratedImages.CountAsync(i => i.TaskId == taskId && i.Status == "success");
            var successTexts = await db.GeneratedTexts.CountAsync(t => t.TaskId == taskId && t.Status == "success");
            var failedImages = await db.GeneratedImages.CountAsync(i => i.TaskId == taskId && i.Status == "failed");
            var failedTexts = await db.GeneratedTexts.CountAsync(t => t.TaskId == taskId && t.Status == "failed");
            var completed = successImages + successTexts + failedImages + failedTexts;
            var progress = Math.Min(99, (int)((decimal)completed / totalSteps * 100));
            await UpdateProgressAsync(db, hubContext, taskId, progress,
                $"已完成 {completed}/{totalSteps} (成功 {successImages + successTexts}, 失败 {failedImages + failedTexts})");
            await Task.Delay(2000);
        }

        var actualCost = await db.GeneratedImages.Where(i => i.TaskId == taskId).SumAsync(i => i.Cost)
            + await db.GeneratedTexts.Where(t => t.TaskId == taskId).SumAsync(t => t.Cost);

        task = await db.GenerationTasks.FindAsync(taskId);
        if (task != null)
        {
            task.ActualCost = actualCost;
            task.Status = "completed";
            task.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await budgetService.Release(task.UserId, task.EstimatedCost - actualCost);
        }
        await UpdateProgressAsync(db, hubContext, taskId, 100, "生成完成!");
    }

    private static async Task GenerateImageAsync(ApplicationDbContext db, IHubContext<GenerationHub> hub,
        IImageGenerator imageGenerator, ILogger logger, Guid taskId,
        ImageTemplate template, Product product, bool useRef)
    {
        var idempotencyKey = $"img_{taskId}_{template.Id}";
        if (await db.GeneratedImages.AnyAsync(i => i.IdempotencyKey == idempotencyKey)) return;

        await PushItemStatus(hub, taskId, template.Name, "image", "calling", "正在调用API...");

        try
        {
            var prompt = template.PromptTemplate
                .Replace("{product_name}", product.Name)
                .Replace("{price}", product.Price.ToString())
                .Replace("{selling_points}", string.Join(",", product.SellingPoints));

            string? refUrl = null;
            if (useRef)
            {
                var task = await db.GenerationTasks.FindAsync(taskId);
                refUrl = (await db.ProductImages.FirstOrDefaultAsync(p => p.ProductId == task!.ProductId && p.Type == "reference"))?.Url;
            }

            var result = await RunWithTimeout(ct => imageGenerator.GenerateAsync(new ImageGenerationRequest(
                Prompt: prompt, Style: template.Style, BatchSize: 1, ReferenceImageUrl: refUrl), ct),
                TimeSpan.FromSeconds(90), idempotencyKey);

            var status = result.Success ? "success" : "failed";
            db.GeneratedImages.Add(new GeneratedImage
            {
                TaskId = taskId, ImageTemplateId = template.Id,
                ImageUrl = result.Success && result.ImageUrls?.Any() == true ? result.ImageUrls[0] : "",
                Cost = result.Cost, Status = status,
                ErrorMessage = result.ErrorMessage, IdempotencyKey = idempotencyKey
            });
            await db.SaveChangesAsync();
            await PushItemStatus(hub, taskId, template.Name, "image", status,
                result.Success ? $"生成成功 (¥{result.Cost:F2})" : $"失败: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Image job failed: {Key}", idempotencyKey);
            db.GeneratedImages.Add(new GeneratedImage
            {
                TaskId = taskId, ImageTemplateId = template.Id,
                Cost = 0, Status = "failed", ErrorMessage = ex.Message, IdempotencyKey = idempotencyKey
            });
            await db.SaveChangesAsync();
            await PushItemStatus(hub, taskId, template.Name, "image", "failed", $"异常: {ex.Message}");
        }
    }

    private static async Task GenerateTextAsync(ApplicationDbContext db, IHubContext<GenerationHub> hub,
        ITextGenerator textGenerator, ILogger logger, Guid taskId,
        CopywritingTemplate template, Product product)
    {
        var idempotencyKey = $"txt_{taskId}_{template.Id}";
        if (await db.GeneratedTexts.AnyAsync(t => t.IdempotencyKey == idempotencyKey)) return;

        await PushItemStatus(hub, taskId, template.Name, "text", "calling", "正在调用API...");

        try
        {
            var productInfo = new Dictionary<string, string>
            {
                ["product_name"] = product.Name,
                ["price"] = product.Price.ToString(),
                ["selling_points"] = string.Join(",", product.SellingPoints)
            };

            var result = await RunWithTimeout(ct => textGenerator.GenerateAsync(
                new TextGenerationRequest(ProductInfo: productInfo, TemplateType: template.TemplateType), ct),
                TimeSpan.FromSeconds(60), idempotencyKey);

            var status = result.Success ? "success" : "failed";
            db.GeneratedTexts.Add(new GeneratedText
            {
                TaskId = taskId, CopywritingTemplateId = template.Id,
                Content = result.Success && result.Texts?.Any() == true ? string.Join("\n", result.Texts) : "",
                Cost = result.Cost, Status = status,
                ErrorMessage = result.ErrorMessage, IdempotencyKey = idempotencyKey
            });
            await db.SaveChangesAsync();
            await PushItemStatus(hub, taskId, template.Name, "text", status,
                result.Success ? $"生成成功 (¥{result.Cost:F2})" : $"失败: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Text job failed: {Key}", idempotencyKey);
            db.GeneratedTexts.Add(new GeneratedText
            {
                TaskId = taskId, CopywritingTemplateId = template.Id,
                Cost = 0, Status = "failed", ErrorMessage = ex.Message, IdempotencyKey = idempotencyKey
            });
            await db.SaveChangesAsync();
            await PushItemStatus(hub, taskId, template.Name, "text", "failed", $"异常: {ex.Message}");
        }
    }

    private static async Task UpdateProgressAsync(ApplicationDbContext db, IHubContext<GenerationHub> hub,
        Guid taskId, int progress, string message)
    {
        var task = await db.GenerationTasks.FindAsync(taskId);
        if (task != null) { task.Progress = progress; task.StatusMessage = message; await db.SaveChangesAsync(); }
        await hub.Clients.Group($"task_{taskId}").SendAsync("TaskProgressUpdated", new
        {
            taskId = taskId.ToString(), progress,
            status = progress >= 100 ? "completed" : "processing", message
        });
    }

    private static async Task PushItemStatus(IHubContext<GenerationHub> hub, Guid taskId,
        string name, string type, string status, string detail)
    {
        await hub.Clients.Group($"task_{taskId}").SendAsync("ItemStatusUpdated", new
        {
            taskId = taskId.ToString(), name, type, status, detail, time = DateTime.UtcNow
        });
    }

    private static async Task<T> RunWithTimeout<T>(Func<CancellationToken, Task<T>> func, TimeSpan timeout, string key)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await func(cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Item {key} timed out after {timeout.TotalSeconds}s");
        }
    }

    private static GenerationTaskResponse MapToResponse(GenerationTask task)
    {
        return new GenerationTaskResponse
        {
            Id = task.Id, ProductId = task.ProductId,
            Status = task.Status, Progress = task.Progress, StatusMessage = task.StatusMessage,
            ImageCount = task.ImageCount, TextVariantsCount = task.TextVariantsCount,
            EstimatedCost = task.EstimatedCost, ActualCost = task.ActualCost,
            CreatedAt = task.CreatedAt
        };
    }
}
