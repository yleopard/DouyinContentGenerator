using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Infrastructure.AI.ImageGenerators;
using DouyinContentGenerator.Infrastructure.AI.TextGenerators;

namespace DouyinContentGenerator.Infrastructure.AI;

public static class AIFactory
{
    public static IServiceCollection AddAIService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<ICostCalculator, CostCalculator>();

        // Image Generator
        services.AddSingleton<IImageGenerator>(sp =>
        {
            var activeProvider = configuration["AIProviders:ImageGeneration:ActiveProvider"] ?? "tongyi_wanxiang";
            return activeProvider switch
            {
                "tongyi_wanxiang" => CreateTongyiWanxiangGenerator(configuration),
                "glm_cogview" => CreateGlmImageGenerator(configuration),
                "xiaomi_mimo" => CreateXiaomiImageGenerator(configuration),
                "baidu_yige" => CreateBaiduYigeGenerator(configuration),
                "bytedance_seedance" => CreateSeedanceGenerator(configuration),
                _ => throw new InvalidOperationException($"Unknown image provider: {activeProvider}")
            };
        });

        // Text Generator
        services.AddSingleton<ITextGenerator>(sp =>
        {
            var activeProvider = configuration["AIProviders:TextGeneration:ActiveProvider"] ?? "tongyi_qianwen";
            return activeProvider switch
            {
                "tongyi_qianwen" => CreateTongyiQianwenGenerator(configuration),
                "glm" => CreateGlmTextGenerator(configuration),
                "xiaomi_mimo" => CreateXiaomiTextGenerator(configuration),
                "baidu_yiyan" => CreateBaiduYiyanGenerator(configuration),
                "bytedance_doubao" => CreateDoubaoGenerator(configuration),
                _ => throw new InvalidOperationException($"Unknown text provider: {activeProvider}")
            };
        });

        return services;
    }

    // === Image Generators ===

    private static TongyiWanxiangGenerator CreateTongyiWanxiangGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:TongyiWanxiang:ApiKey"]
            ?? throw new Exception("TongyiWanxiang API Key not configured");
        var model = configuration["AIProviders:TongyiWanxiang:Model"] ?? "wan2.1-t2i-turbo";
        return new TongyiWanxiangGenerator(apiKey, model);
    }

    private static GlmImageGenerator CreateGlmImageGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:GlmCogView:ApiKey"]
            ?? throw new Exception("GLM CogView API Key not configured");
        var model = configuration["AIProviders:GlmCogView:Model"] ?? "glm-image";
        return new GlmImageGenerator(apiKey, model);
    }

    private static XiaomiImageGenerator CreateXiaomiImageGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:XiaomiMiMo:ApiKey"]
            ?? throw new Exception("Xiaomi MiMo API Key not configured");
        var model = configuration["AIProviders:XiaomiMiMo:Model"] ?? "MiMo-V2-Flash";
        return new XiaomiImageGenerator(apiKey, model);
    }

    // === Text Generators ===

    private static TongyiQianwenGenerator CreateTongyiQianwenGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:TongyiQianwen:ApiKey"]
            ?? throw new Exception("TongyiQianwen API Key not configured");
        var model = configuration["AIProviders:TongyiQianwen:Model"] ?? "qwen-turbo";
        return new TongyiQianwenGenerator(apiKey, model);
    }

    private static GlmTextGenerator CreateGlmTextGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:Glm:ApiKey"]
            ?? throw new Exception("GLM API Key not configured");
        var model = configuration["AIProviders:Glm:Model"] ?? "glm-4-flash";
        return new GlmTextGenerator(apiKey, model);
    }

    private static XiaomiTextGenerator CreateXiaomiTextGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:XiaomiMiMo:ApiKey"] ?? "placeholder-xiaomi-key";
        var model = configuration["AIProviders:XiaomiMiMo:Model"] ?? "mimo-v2.5-pro";
        return new XiaomiTextGenerator(apiKey, model);
    }

    private static BaiduYigeGenerator CreateBaiduYigeGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:BaiduYige:ApiKey"] ?? "placeholder-baidu-key";
        var secretKey = configuration["AIProviders:BaiduYige:SecretKey"] ?? apiKey;
        var model = configuration["AIProviders:BaiduYige:Model"] ?? "sd_xl";
        return new BaiduYigeGenerator(apiKey, model, secretKey);
    }

    private static ByteDanceSeedanceGenerator CreateSeedanceGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:ByteDanceSeedance:ApiKey"] ?? "placeholder-seedance-key";
        var model = configuration["AIProviders:ByteDanceSeedance:Model"] ?? "doubao-seedream-4-0-250828";
        return new ByteDanceSeedanceGenerator(apiKey, model);
    }

    private static BaiduYiyanGenerator CreateBaiduYiyanGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:BaiduYiyan:ApiKey"] ?? "placeholder-baidu-key";
        var secretKey = configuration["AIProviders:BaiduYiyan:SecretKey"] ?? apiKey;
        var model = configuration["AIProviders:BaiduYiyan:Model"] ?? "ernie-4.0-turbo";
        return new BaiduYiyanGenerator(apiKey, model, secretKey);
    }

    private static ByteDanceDoubaoGenerator CreateDoubaoGenerator(IConfiguration configuration)
    {
        var apiKey = configuration["AIProviders:ByteDanceDoubao:ApiKey"] ?? "placeholder-doubao-key";
        var model = configuration["AIProviders:ByteDanceDoubao:Model"] ?? "doubao-pro-32k";
        return new ByteDanceDoubaoGenerator(apiKey, model);
    }
}
