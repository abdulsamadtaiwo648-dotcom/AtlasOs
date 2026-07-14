using Atlas.Core.Interfaces;

namespace Atlas.Core.Commands;

public class HistoryCommand : ICommand
{
    private readonly IConversationStore _store;

    public HistoryCommand(IConversationStore store)
    {
        _store = store;
    }

    public string Name => "/history";

    public string Description => "Shows all saved conversations.";

    public string Execute(string[] args)
    {
        var conversations = _store.GetAll();

        if (conversations.Count == 0)
            return "No conversations found.";

        List<string> output = new();

        foreach (var conversation in conversations)
        {
            output.Add(
                $"ID: {conversation.Id} | Created: {conversation.CreatedAt:g}");
        }

        return string.Join(Environment.NewLine, output);
    }
}