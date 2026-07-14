namespace Atlas.Core.Commands;
using Atlas.Core.Interfaces;

public class DeleteCommand : ICommand
{
    public string Name => "/delete";

    public string Description => "Deletes a conversation.";

    public string Execute(string[] args)
    {
        return "Delete is not implemented yet.";
    }
}