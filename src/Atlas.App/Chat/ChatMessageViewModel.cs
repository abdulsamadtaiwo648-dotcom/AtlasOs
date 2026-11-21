using Atlas.App.Mvvm;

namespace Atlas.App.Chat;

public enum ChatRole
{
    User,
    Assistant,
    System
}

public sealed class ChatMessageViewModel : ObservableObject
{
    private string _text;
    private bool _isStreaming;

    public ChatMessageViewModel(ChatRole role, string text, DateTime? timestamp = null)
    {
        Role = role;
        _text = text;
        Timestamp = timestamp ?? DateTime.Now;
    }

    public ChatRole Role { get; }

    public bool IsUser => Role == ChatRole.User;
    public bool IsAssistant => Role == ChatRole.Assistant;
    public bool IsSystem => Role == ChatRole.System;

    public DateTime Timestamp { get; }

    public string TimeText => Timestamp.ToString("HH:mm");

    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }

    /// <summary>True while the reply is still being written.</summary>
    public bool IsStreaming
    {
        get => _isStreaming;
        set => Set(ref _isStreaming, value);
    }
}

public sealed record ConversationSummary(Guid Id, string Title, DateTime CreatedAt)
{
    public string DateText => CreatedAt.ToLocalTime().ToString("d MMM");
}
