using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Infrastructure.AI.ImageGenerators;
using DouyinContentGenerator.Infrastructure.AI.TextGenerators;
using DouyinContentGenerator.Infrastructure.Data;
using DouyinContentGenerator.Core.Models;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/ai-providers")]
[Authorize]
public class AIProvidersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AIProvidersController(ApplicationDbContext db) { _db = db; }

    /// <summary>Load current user's AI provider settings</summary>
    [HttpGet("settings")]
    public async Task<ActionResult> GetSettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _db.UserAISettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null) return Ok(new { configJson = "{}" });
        return Ok(new { settings.Id, configJson = settings.ConfigJson, settings.UpdatedAt });
    }

    /// <summary>Save current user's AI provider settings</summary>
    [HttpPut("settings")]
    public async Task<ActionResult> SaveSettings([FromBody] SaveSettingsRequest req)
    {
        var userId = GetCurrentUserId();
        var settings = await _db.UserAISettings.FirstOrDefaultAsync(s => s.UserId == userId);
        if (settings == null)
        {
            settings = new UserAISettings { UserId = userId, ConfigJson = req.ConfigJson };
            _db.UserAISettings.Add(settings);
        }
        else
        {
            settings.ConfigJson = req.ConfigJson;
            settings.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(new { settings.Id, settings.UpdatedAt });
    }

    /// <summary>
    /// Test an AI provider by sending a real request and returning a sample output.
    /// For text: sends "用一句话介绍水杯" and returns the generated text.
    /// For image: sends "a simple red cup on a white background" and returns the image URL.
    /// Also returns the actual endpoint URL for diagnostics.
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult> TestConnection([FromBody] TestProviderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new { error = "API Key is required", success = false });

        try
        {
            var startTime = DateTime.UtcNow;

            var isImage = request.Type is "tongyi_wanxiang" or "glm_cogview" or "xiaomi_mimo_image";

            // Map provider to endpoint info for diagnostics
            var endpointInfo = GetEndpointInfo(request.Type, request.Model);

            var result = request.Type switch
            {
                "tongyi_wanxiang" => await TestImageGenerator(new TongyiWanxiangGenerator(request.ApiKey, request.Model ?? "wan2.1-t2i-turbo")),
                "glm_cogview" => await TestImageGenerator(new GlmImageGenerator(request.ApiKey, request.Model ?? "glm-image")),
                "baidu_yige" => await TestImageGenerator(new BaiduYigeGenerator(request.ApiKey, request.Model ?? "sd_xl")),
                "bytedance_seedance" => await TestImageGenerator(new ByteDanceSeedanceGenerator(request.ApiKey, request.Model ?? "seedream-4.0")),
                "xiaomi_mimo_image" => await TestImageGenerator(new XiaomiImageGenerator(request.ApiKey, request.Model ?? "mimo-v2.5-pro")),
                "tongyi_qianwen" => await TestTextGenerator(new TongyiQianwenGenerator(request.ApiKey, request.Model ?? "qwen-turbo")),
                "glm" => await TestTextGenerator(new GlmTextGenerator(request.ApiKey, request.Model ?? "glm-4-flash")),
                "baidu_yiyan" => await TestTextGenerator(new BaiduYiyanGenerator(request.ApiKey, request.Model ?? "ernie-4.0-turbo")),
                "bytedance_doubao" => await TestTextGenerator(new ByteDanceDoubaoGenerator(request.ApiKey, request.Model ?? "doubao-pro-32k")),
                "xiaomi_mimo_text" => await TestTextGenerator(new XiaomiTextGenerator(request.ApiKey, request.Model ?? "mimo-v2.5-pro")),
                _ => throw new ArgumentException($"Unknown provider type: {request.Type}")
            };

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;

            return Ok(new
            {
                success = result.Success,
                message = result.Success ? (isImage ? $"图片生成成功，耗时 {duration:F1}s" : $"文案生成成功，耗时 {duration:F1}s")
                                         : $"测试失败: {result.ErrorMessage}",
                provider = request.Type,
                model = request.Model,
                durationSeconds = Math.Round(duration, 2),
                sample = result.Sample,
                sampleType = isImage ? "image" : "text",
                cost = Math.Round(result.Cost, 4),
                endpoint = endpointInfo  // Show the API URL being called
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                message = $"系统异常: {ex.Message}",
                provider = request.Type,
                model = request.Model
            });
        }
    }

    private static string GetEndpointInfo(string type, string? model) => type switch
    {
        "tongyi_wanxiang" => "dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis",
        "tongyi_qianwen" => "dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
        "glm_cogview" or "glm" => "open.bigmodel.cn/api/paas/v4/images/generations",
        "baidu_yige" => "aip.baidubce.com/rpc/2.0/.../text2image/sd_xl",
        "baidu_yiyan" => "aip.baidubce.com/rpc/2.0/.../chat/ernie-4.0-turbo",
        "bytedance_seedance" => "ark.cn-beijing.volces.com/api/v3/images/generations",
        "bytedance_doubao" => "ark.cn-beijing.volces.com/api/v3/chat/completions",
        "xiaomi_mimo_image" or "xiaomi_mimo_text" => "api.xiaomimimo.com/anthropic/v1/messages (api-key)",
        _ => "unknown"
    };

    // === Real test: generate a sample text ===
    private static async Task<(bool Success, string? Sample, string? ErrorMessage, decimal Cost)> TestTextGenerator(
        Core.Interfaces.ITextGenerator generator)
    {
        try
        {
            var request = new TextGenerationRequest(
                ProductInfo: new Dictionary<string, string> { ["product_name"] = "水杯", ["price"] = "29.9", ["selling_points"] = "大容量" },
                TemplateType: "pain_point",
                MaxLength: 200);

            var result = await generator.GenerateAsync(request);

            if (result.Success && result.Texts?.Count > 0)
            {
                var sample = result.Texts[0];
                if (sample.Length > 300) sample = sample[..300] + "...";
                return (true, sample, null, result.Cost);
            }

            return (false, null, result.ErrorMessage ?? "未返回任何内容", result.Cost);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message, 0);
        }
    }

    // === Real test: generate a sample image ===
    private static async Task<(bool Success, string? Sample, string? ErrorMessage, decimal Cost)> TestImageGenerator(
        Core.Interfaces.IImageGenerator generator)
    {
        try
        {
            var request = new ImageGenerationRequest(
                Prompt: "一只简约的红色水杯，白色背景，产品摄影风格",
                Style: "product_photography",
                BatchSize: 1,
                Size: "1280x1280");

            var result = await generator.GenerateAsync(request);

            if (result.Success && result.ImageUrls?.Count > 0)
                return (true, result.ImageUrls[0], null, result.Cost);

            return (false, null, result.ErrorMessage ?? "未生成图片", result.Cost);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message, 0);
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim?.Value ?? throw new UnauthorizedAccessException());
    }
}

public class TestProviderRequest
{
    public string Type { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? Model { get; set; }
}

public class SaveSettingsRequest
{
    public string ConfigJson { get; set; } = "{}";
}
