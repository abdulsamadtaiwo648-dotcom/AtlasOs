using Atlas.Core.Models;

namespace Atlas.Core.Interfaces;

/// <summary>Long-term memory: things Atlas has been told and should recall later.</summary>
public interface IMemoryService
{
    /// <summary>Stores a memory. Saying the same thing twice updates the existing entry instead of duplicating it.</summary>
    MemoryEntry Remember(
        string content,
        MemoryKind kind = MemoryKind.Fact,
        IEnumerable<string>? tags = null,
        int importance = 3,
        string source = "user");

    /// <summary>Finds the memories most relevant to the text, best first.</summary>
    IReadOnlyList<MemoryEntry> Search(string query, int limit = 5);

    /// <summary>Lists memories, newest first.</summary>
    IReadOnlyList<MemoryEntry> List(MemoryKind? kind = null, int limit = 50);

    /// <summary>Entries marked as always important (importance 5).</summary>
    IReadOnlyList<MemoryEntry> Pinned(int limit = 3);

    MemoryEntry? Get(long id);

    /// <summary>Sets how important a memory is (1-5). 5 means "always keep in mind".</summary>
    bool SetImportance(long id, int importance);

    bool Forget(long id);

    int Count();
}
