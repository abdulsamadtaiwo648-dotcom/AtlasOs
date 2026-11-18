using Atlas.Core.Enums;

namespace Atlas.Core.Models;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Event { get; set; } = "";
    public Dictionary<string, string> Metadata { get; set; } = new();
}
