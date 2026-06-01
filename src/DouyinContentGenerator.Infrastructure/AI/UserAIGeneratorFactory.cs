using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Infrastructure.AI.ImageGenerators;
using DouyinContentGenerator.Infrastructure.AI.TextGenerators;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.Infrastructure.AI;

public class UserAIGeneratorFactory : IUserAIGeneratorFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public UserAIGeneratorFactory(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _config = config;
    }

    public async Task<(IImageGenerator Generator, string Provider, string Model)> CreateImageGeneratorAsync(Guid userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = await db.UserAISettings.FirstOrDefaultAsync(s => s.UserId == userId);
        var configJson = settings?.ConfigJson;

        using var doc = JsonDocument.Parse(configJson ?? "{}");
        var root = doc.RootElement;

        var provider = GetJsonString(root, "imageProvider") ?? _config["AIProviders:ImageGeneration:ActiveProvider"] ?? "tongyi_wanxiang";
        var apiKey = GetJsonString(root, "imageApiKey") ?? _config["AIProviders:TongyiWanxiang:ApiKey"] ?? "placeholder-tongyi-key";
        var model = GetJsonString(root, "imageModel") ?? _config["AIProviders:TongyiWanxiang:Model"] ?? "glm-image";

        var generator = provider switch
        {
            "tongyi_wanxiang" => (IImageGenerator)new TongyiWanxiangGenerator(apiKey, model),
            "glm_cogview" => new GlmImageGenerator(apiKey, model),
            "baidu_yige" => new BaiduYigeGenerator(apiKey, model),
            "bytedance_seedance" => new ByteDanceSeedanceGenerator(apiKey, model),
            "xiaomi_mimo" => new XiaomiImageGenerator(apiKey, model ?? "mimo-v2.5-pro"),
            _ => new TongyiWanxiangGenerator(apiKey, model)
        };

        return (generator, provider, model);
    }

    public async Task<(ITextGenerator Generator, string Provider, string Model)> CreateTextGeneratorAsync(Guid userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var settings = await db.UserAISettings.FirstOrDefaultAsync(s => s.UserId == userId);
        var configJson = settings?.ConfigJson;

        using var doc = JsonDocument.Parse(configJson ?? "{}");
        var root = doc.RootElement;

        var provider = GetJsonString(root, "textProvider") ?? _config["AIProviders:TextGeneration:ActiveProvider"] ?? "tongyi_qianwen";
        var apiKey = GetJsonString(root, "textApiKey") ?? _config["AIProviders:TongyiQianwen:ApiKey"] ?? "placeholder-tongyi-key";
        var model = GetJsonString(root, "textModel") ?? _config["AIProviders:TongyiQianwen:Model"] ?? "qwen-turbo";

        var generator = provider switch
        {
            "tongyi_qianwen" => (ITextGenerator)new TongyiQianwenGenerator(apiKey, model),
            "glm" => new GlmTextGenerator(apiKey, model),
            "baidu_yiyan" => new BaiduYiyanGenerator(apiKey, model ?? "ernie-4.0-turbo"),
            "bytedance_doubao" => new ByteDanceDoubaoGenerator(apiKey, model ?? "doubao-pro-32k"),
            "xiaomi_mimo" or "xiaomi_mimo_text" => new XiaomiTextGenerator(apiKey, model),
            _ => new TongyiQianwenGenerator(apiKey, model)
        };

        return (generator, provider, model);
    }

    private static string? GetJsonString(JsonElement root, string key)
    {
        return root.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String && el.GetString() is { Length: > 0 } val ? val : null;
    }
}
