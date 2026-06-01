namespace DouyinContentGenerator.Core.Interfaces;

public interface ICostCalculator
{
    decimal CalculateImageCost(string providerName, string model, string size, bool useReference);

    decimal CalculateTextCost(string providerName, string model, int tokenCount);

    (decimal imageCost, decimal textCost) EstimateTaskCost(
        int imageCount,
        int textVariantsCount,
        string imageProvider,
        string textProvider,
        bool useReference);
}
