using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.Core.Commands;

/// <summary>/memory — list what Atlas remembers.  /memory search words — find memories.</summary>
public class MemoryCommand : ICommand
{
    private readonly IMemoryService _memory;

    public MemoryCommand(IMemoryService memory)
    {
        _memory = memory;
    }

    public string Name => "/memory";

    public string Description => "Lists what Atlas remembers (/memory search <words> to look something up).";

    public string Execute(string[] args)
    {
        IReadOnlyList<MemoryEntry> entries =
            args.Length > 0 && args[0].Equals("search", StringComparison.OrdinalIgnoreCase)
                ? _memory.Search(string.Join(' ', args.Skip(1)), 10)
                : _memory.List(limit: 30);

        if (entries.Count == 0)
            return "No memories found.";

        return string.Join("\n", entries.Select(e => $"  [{e.Id}] {e.Content}"))
               + $"\n\n{_memory.Count()} {(_memory.Count() == 1 ? "memory" : "memories")} stored. Use /forget <id> to remove one.";
    }
}

public class ForgetCommand : ICommand
{
    private readonly IMemoryService _memory;

    public ForgetCommand(IMemoryService memory)
    {
        _memory = memory;
    }

    public string Name => "/forget";

    public string Description => "Removes a memory by id (see /memory).";

    public string Execute(string[] args)
    {
        if (args.Length == 0 || !long.TryParse(args[0], out long id))
            return "Usage: /forget <id>   (use /memory to see ids)";

        MemoryEntry? entry = _memory.Get(id);
        if (entry == null)
            return $"No memory with id {id}.";

        _memory.Forget(id);
        return $"Forgot: {entry.Content}";
    }
}
