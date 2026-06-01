using Xunit;
using FluentAssertions;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Infrastructure.AI.ImageGenerators;

namespace DouyinContentGenerator.Tests.Unit.AI;

public class TongyiWanxiangGeneratorTests
{
    [Fact]
    public void GetCostPerImage_ShouldReturnCorrectPrice_ForTurboModel()
    {
        var generator = new TongyiWanxiangGenerator("test-key", "wan2.1-t2i-turbo");

        var cost = generator.GetCostPerImage(false);

        cost.Should().Be(0.18m);
    }

    [Fact]
    public void GetCostPerImage_ShouldApplyMultiplier_ForReferenceImage()
    {
        var generator = new TongyiWanxiangGenerator("test-key", "wan2.1-t2i-turbo");

        var cost = generator.GetCostPerImage(true);

        cost.Should().Be(0.216m);
    }
}
