using Atlas.Tools.Interfaces;
using Atlas.Tools.Services;

namespace Atlas.Tests;

public class ToolTests
{
    [Theory]
    [InlineData("calculate 2+2*5", "12")]
    [InlineData("CALCULATE (10+5)/3", "5")]
    public void Calculator_EvaluatesExpressions(string input, string expected)
    {
        Assert.Equal(expected, new CalculatorTool().Execute(input));
    }

    [Fact]
    public void Calculator_ReturnsMessageForInvalidExpression()
    {
        Assert.Equal("Invalid expression.", new CalculatorTool().Execute("calculate 2 +* banana"));
    }

    [Fact]
    public void Calculator_OnlyHandlesCalculatePrefix()
    {
        CalculatorTool tool = new();

        Assert.True(tool.CanHandle("calculate 1+1"));
        Assert.False(tool.CanHandle("hello"));
    }

    [Fact]
    public void ToolManager_RunsFirstMatchingTool()
    {
        ToolManager manager = new(new ITool[] { new CalculatorTool() });

        Assert.True(manager.TryExecute("calculate 3*3", out string result));
        Assert.Equal("9", result);
    }

    [Fact]
    public void ToolManager_ReturnsFalseWhenNoToolMatches()
    {
        ToolManager manager = new(new ITool[] { new CalculatorTool() });

        Assert.False(manager.TryExecute("what's the weather", out string result));
        Assert.Equal("", result);
    }
}
