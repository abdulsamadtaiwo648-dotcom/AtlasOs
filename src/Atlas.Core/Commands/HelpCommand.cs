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
/memory    List what Atlas remembers (/memory search <words>)
/forget    Remove a memory by id
/exit      Exit Atlas

You can also say: "remember that ...", "forget ...", "what do you remember about ...".
""";
    }
}