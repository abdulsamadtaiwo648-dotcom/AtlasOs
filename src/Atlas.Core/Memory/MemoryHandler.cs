using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.Core.Memory;

/// <summary>Carries out a <see cref="MemoryRequest"/> and returns what Atlas should say.</summary>
public sealed class MemoryHandler
{
    private readonly IMemoryService _memory;

    public MemoryHandler(IMemoryService memory)
    {
        _memory = memory;
    }

    public string Handle(MemoryRequest request)
    {
        switch (request.Action)
        {
            case MemoryAction.Remember:
            {
                int before = _memory.Count();
                MemoryEntry entry = _memory.Remember(request.Text, request.Kind, source: "user");
                return _memory.Count() > before
                    ? $"Got it. I'll remember that: {entry.Content}"
                    : $"I already knew that: {entry.Content}";
            }

            case MemoryAction.Forget:
            {
                MemoryEntry? match = _memory.Search(request.Text, 1).FirstOrDefault();
                if (match == null)
                    return $"I don't have anything stored about \"{request.Text}\".";

                _memory.Forget(match.Id);
                return $"Okay, I've forgotten: {match.Content}";
            }

            case MemoryAction.RecallTopic:
            {
                IReadOnlyList<MemoryEntry> found = _memory.Search(request.Text, 5);
                return found.Count == 0
                    ? $"I don't have anything stored about {request.Text}."
                    : $"Here's what I remember about {request.Text}:\n" + Format(found);
            }

            default:
            {
                IReadOnlyList<MemoryEntry> all = _memory.List(limit: 20);
                return all.Count == 0
                    ? "I don't have any memories yet. Tell me to remember something."
                    : "Here's what I remember:\n" + Format(all);
            }
        }
    }

    public static string Format(IEnumerable<MemoryEntry> entries) =>
        string.Join("\n", entries.Select(e => $"  • {e.Content}"));
}
