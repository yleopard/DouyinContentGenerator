using Xunit;
using FluentAssertions;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Infrastructure.AI.TextGenerators;

namespace DouyinContentGenerator.Tests.Unit.AI;

public class TongyiQianwenGeneratorTests
{
    [Fact]
    public void GetCostPerToken_ShouldReturnCorrectPrice_ForTurboModel()
    {
        var generator = new TongyiQianwenGenerator("test-key", "qwen-turbo");

        var cost = generator.GetCostPerToken();

        cost.Should().Be(0.000002m);
    }

    [Fact]
    public void Generator_ShouldBeCreated_WithDefaultValues()
    {
        var generator = new TongyiQianwenGenerator("test-key");

        generator.Should().NotBeNull();
        generator.ProviderName.Should().Be("通义千问");
    }
}
