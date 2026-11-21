using Atlas.Core.Enums;

namespace Atlas.Core.Models;

public static class ConversationExtensions
{
    /// <summary>A short title for the chat list: the first thing the user said.</summary>
    public static string Title(this Conversation conversation)
    {
        string? first = conversation.Messages
            .FirstOrDefault(m => m.Role == MessageRole.User)?.Content.Trim();

        if (string.IsNullOrEmpty(first))
            return "New chat";

        first = first.ReplaceLineEndings(" ");
        return first.Length <= 40 ? first : first[..37].TrimEnd() + "...";
    }
}
