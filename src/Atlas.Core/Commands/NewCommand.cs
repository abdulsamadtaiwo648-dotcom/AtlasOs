namespace Atlas.Core.Commands;
using Atlas.Core.Interfaces;

public class NewCommand : ICommand
{
    public string Name => "/new";

    public string Description => "Starts a new conversation.";

    public string Execute(string[] args)
    {
        return "New conversation created.";
    }
}