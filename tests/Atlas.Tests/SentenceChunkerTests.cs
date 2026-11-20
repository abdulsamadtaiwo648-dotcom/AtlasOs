using Atlas.Speech.Loop;

namespace Atlas.Tests;

public class SentenceChunkerTests
{
    private static List<string> Run(params string[] fragments)
    {
        SentenceChunker chunker = new();
        List<string> output = new();

        foreach (string f in fragments)
            output.AddRange(chunker.Push(f));

        output.AddRange(chunker.Flush());
        return output;
    }

    [Fact]
    public void SplitsIntoSentences_AcrossFragments()
    {
        List<string> result = Run("The weather today is su", "nny and warm. Tomorrow it will ", "rain a lot! Bring an umbrella please.");

        Assert.Equal(
            new[] { "The weather today is sunny and warm.", "Tomorrow it will rain a lot!", "Bring an umbrella please." },
            result);
    }

    [Fact]
    public void DoesNotSplitOnDecimalsOrAbbreviationsMidWord()
    {
        List<string> result = Run("The price is 3.5 dollars today and rising fast.");

        Assert.Single(result);
    }

    [Fact]
    public void FlushReturnsTrailingTextWithoutPunctuation()
    {
        List<string> result = Run("Sure, I can help with that");

        Assert.Equal(new[] { "Sure, I can help with that" }, result);
    }

    [Fact]
    public void ShortFragmentsAreNotEmittedAsTheirOwnSentence()
    {
        List<string> result = Run("Yes. It is going to be a really nice day today.");

        Assert.Single(result);
    }

    [Fact]
    public void NewlineEndsASentence()
    {
        List<string> result = Run("First point here\nSecond point here");

        Assert.Equal(new[] { "First point here", "Second point here" }, result);
    }

    [Fact]
    public void VeryLongTextWithoutPunctuationIsStillCut()
    {
        string longText = string.Join(" ", Enumerable.Repeat("word", 120));

        List<string> result = Run(longText);

        Assert.True(result.Count > 1);
        Assert.All(result, s => Assert.True(s.Length <= 260));
    }
}
