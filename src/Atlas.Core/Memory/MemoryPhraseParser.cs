using System.Text.RegularExpressions;
using Atlas.Core.Models;

namespace Atlas.Core.Memory;

public enum MemoryAction
{
    Remember,
    Forget,
    RecallTopic,
    RecallAll
}

public sealed record MemoryRequest(MemoryAction Action, string Text, MemoryKind Kind = MemoryKind.Fact);

/// <summary>
/// Recognises natural phrases about memory: "remember that...", "forget...",
/// "what do you remember about...". Pure text logic, so it also works for voice.
/// </summary>
public static class MemoryPhraseParser
{
    private static readonly Regex Remember = new(
        @"^\s*(?:please\s+|atlas[, ]+)?(?:remember|keep in mind|note)\s+(?:that\s+)?(?<text>\S.*?)\s*[.!]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Forget = new(
        @"^\s*(?:please\s+|atlas[, ]+)?forget\s+(?:about\s+|that\s+)?(?<text>\S.*?)\s*[.!]?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RecallTopic = new(
        @"^\s*(?:atlas[, ]+)?(?:what|do)\s+(?:do\s+)?you\s+(?:remember|know)\s+about\s+(?<text>\S.*?)\s*\??\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RecallAll = new(
        @"^\s*(?:atlas[, ]+)?(?:what\s+do\s+you\s+(?:remember|know)(?:\s+about\s+me)?|(?:show|list)\s+(?:me\s+)?(?:my\s+|all\s+)?memories)\s*\??\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PreferenceHint = new(
        @"\b(i\s+(?:really\s+)?(?:like|love|prefer|enjoy|hate|dislike)|favou?rite|i\s+don'?t\s+like)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static MemoryRequest? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.TrimStart().StartsWith('/'))
            return null;

        if (RecallAll.IsMatch(input))
            return new MemoryRequest(MemoryAction.RecallAll, "");

        Match topic = RecallTopic.Match(input);
        if (topic.Success)
        {
            string text = topic.Groups["text"].Value;
            return text.Equals("me", StringComparison.OrdinalIgnoreCase)
                ? new MemoryRequest(MemoryAction.RecallAll, "")
                : new MemoryRequest(MemoryAction.RecallTopic, text);
        }

        Match forget = Forget.Match(input);
        if (forget.Success)
        {
            string text = forget.Groups["text"].Value;

            // "forget it" / "forget that" is conversation, not a memory request
            if (text.Equals("it", StringComparison.OrdinalIgnoreCase))
                return null;

            return new MemoryRequest(MemoryAction.Forget, text);
        }

        Match remember = Remember.Match(input);
        if (remember.Success)
        {
            string text = remember.Groups["text"].Value;
            MemoryKind kind = PreferenceHint.IsMatch(text) ? MemoryKind.Preference : MemoryKind.Fact;
            return new MemoryRequest(MemoryAction.Remember, text, kind);
        }

        return null;
    }
}
