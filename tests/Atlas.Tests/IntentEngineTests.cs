using Atlas.Core.Intent;

namespace Atlas.Tests;

public class IntentEngineTests
{
    private readonly IntentEngine _engine = new();

    [Theory]
    [InlineData("/help", IntentType.Command)]
    [InlineData("calculate 2+2", IntentType.Tool)]
    [InlineData("(10+5)/3", IntentType.Tool)]
    [InlineData("what is the bitcoin price", IntentType.Finance)]
    [InlineData("analyze eurusd order block", IntentType.Finance)]
    [InlineData("turn on the living room lamp", IntentType.SmartHome)]
    [InlineData("open the garage door", IntentType.SmartHome)]
    [InlineData("launch notepad", IntentType.System)]
    [InlineData("check the security camera", IntentType.Vision)]
    [InlineData("tell me a joke", IntentType.Conversation)]
    public void Detect_ClassifiesInput(string input, IntentType expected)
    {
        Assert.Equal(expected, _engine.Detect(input).Type);
    }

    [Fact]
    public void Detect_IsCaseInsensitiveAndKeepsOriginalInput()
    {
        IntentResult result = _engine.Detect("  BITCOIN price  ");

        Assert.Equal(IntentType.Finance, result.Type);
        Assert.Equal("  BITCOIN price  ", result.OriginalInput);
    }

    [Fact]
    public void Detect_CommandHasFullConfidence_ConversationIsLow()
    {
        Assert.Equal(1.0, _engine.Detect("/new").Confidence);
        Assert.True(_engine.Detect("tell me a joke").Confidence < 0.6);
    }
}
