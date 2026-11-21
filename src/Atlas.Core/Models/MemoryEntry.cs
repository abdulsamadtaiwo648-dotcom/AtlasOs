namespace Atlas.Core.Models;

public enum MemoryKind
{
    Fact,
    Preference,
    Note,
    Event
}

/// <summary>One thing Atlas has been told to remember.</summary>
public class MemoryEntry
{
    public long Id { get; set; }
    public MemoryKind Kind { get; set; } = MemoryKind.Fact;
    public string Content { get; set; } = "";
    public List<string> Tags { get; set; } = new();

    /// <summary>1 (minor) to 5 (always keep in mind).</summary>
    public int Importance { get; set; } = 3;

    /// <summary>Where it came from, e.g. "user" or "voice".</summary>
    public string Source { get; set; } = "user";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; }
}
