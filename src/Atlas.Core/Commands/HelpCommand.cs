using Atlas.Core.Interfaces;

namespace Atlas.Core.Commands;

public class HelpCommand : ICommand
{
    public string Name => "/help";

    public string Description => "Shows available commands.";

    public string Execute(string[] args)
    {
        return """
Available Commands

/help      Show this menu
/new       Start a new conversation
/history   List conversations
/load      Load a conversation
/delete    Delete a conversation
/exit      Exit Atlas
""";
    }
}