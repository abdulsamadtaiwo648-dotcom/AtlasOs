using Atlas.Core.Interfaces;

namespace Atlas.Core.Services;

public class CommandDispatcher
{
    private readonly Dictionary<string, ICommand> _commands = new();

    public CommandDispatcher(IEnumerable<ICommand> commands)
    {
        foreach (var command in commands)
        {
            _commands.Add(command.Name, command);
        }
    }

    public bool TryExecute(string input, out string result)
    {
        result = "";

        if (!input.StartsWith("/"))
            return false;

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string name = parts[0];

        string[] args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(name, out ICommand? command))
        {
            result = command.Execute(args);
            return true;
        }

        result = "Unknown command.";

        return true;
    }
}