using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI;

public class CostCalculator : ICostCalculator
{
    private readonly Dictionary<string, decimal> _imagePrices = new()
    {
        ["tongyi_wanxiang:wan2.1-t2i-turbo"] = 0.18m,
        ["tongyi_wanxiang:wan2.1-t2i-plus"] = 0.37m,
        ["glm_cogview:glm-image"] = 0.15m,
        ["glm_cogview:cogview-4"] = 0.30m,
        ["glm_cogview:cogview-3-flash"] = 0.10m,
        ["baidu_yige:sd_xl"] = 0.20m,
        ["bytedance_seedance:doubao-seedream-4-0-250828"] = 0.25m,
        ["xiaomi_mimo:mimo-v2.5-pro"] = 0.30m,
    };

    private readonly Dictionary<string, decimal> _textPrices = new()
    {
        ["tongyi_qianwen:qwen-turbo"] = 0.000002m,
        ["tongyi_qianwen:qwen-plus"] = 0.000004m,
        ["glm:glm-4-flash"] = 0.000001m,
        ["glm:glm-4-plus"] = 0.000014m,
        ["glm:glm-4-air"] = 0.000001m,
        ["xiaomi_mimo:mimo-v2.5-flash"] = 0.000001m,
        ["xiaomi_mimo:mimo-v2.5-pro"] = 0.000002m,
        ["baidu_yiyan:ernie-4.0-turbo"] = 0.000003m,
        ["bytedance_doubao:doubao-pro-32k"] = 0.000002m,
    };

    public decimal CalculateImageCost(string providerName, string model, string size, bool useReference)
    {
        var key = $"{providerName}:{model}";
        var basePrice = _imagePrices.GetValueOrDefault(key, 0.18m);
        return basePrice * (useReference ? 1.2m : 1.0m);
    }

    public decimal CalculateTextCost(string providerName, string model, int tokenCount)
    {
        var key = $"{providerName}:{model}";
        var pricePerToken = _textPrices.GetValueOrDefault(key, 0.000002m);
        return tokenCount * pricePerToken;
    }

    public (decimal imageCost, decimal textCost) EstimateTaskCost(
        int imageCount,
        int textVariantsCount,
        string imageProvider,
        string textProvider,
        bool useReference)
    {
        const int averageTokensPerText = 500;

        var imageCost = CalculateImageCost(imageProvider, "default", "1024x1024", useReference) * imageCount;
        var textCost = CalculateTextCost(textProvider, "default", averageTokensPerText * textVariantsCount);

        return (imageCost, textCost);
    }
}
