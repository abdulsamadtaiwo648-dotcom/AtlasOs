using Atlas.Core.Interfaces;

namespace Atlas.Core.Commands;

public class NewCommand : ICommand
{
    private readonly IConversationService _conversation;

    public NewCommand(IConversationService conversation)
    {
        _conversation = conversation;
    }

    public string Name => "/new";

    public string Description => "Starts a new conversation.";

    public string Execute(string[] args)
    {
        _conversation.StartNew();
        return "New conversation created.";
    }
}
