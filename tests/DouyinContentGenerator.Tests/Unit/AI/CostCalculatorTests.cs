using Xunit;
using FluentAssertions;
using DouyinContentGenerator.Infrastructure.AI;

namespace DouyinContentGenerator.Tests.Unit.AI;

public class CostCalculatorTests
{
    private readonly CostCalculator _calculator = new();

    [Fact]
    public void EstimateTaskCost_ShouldCalculateCorrectCost_ForStandardTask()
    {
        int imageCount = 3;
        int textVariantsCount = 5;

        var (imageCost, textCost) = _calculator.EstimateTaskCost(
            imageCount, textVariantsCount,
            "tongyi_wanxiang", "tongyi_qianwen",
            useReference: false
        );

        imageCost.Should().BeApproximately(0.54m, 0.01m);
        textCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateTaskCost_ShouldApplyMultiplier_ForReferenceImage()
    {
        int imageCount = 3;

        var (imageCost, _) = _calculator.EstimateTaskCost(
            imageCount, 0,
            "tongyi_wanxiang", "tongyi_qianwen",
            useReference: true
        );

        imageCost.Should().BeApproximately(0.648m, 0.01m);
    }
}
