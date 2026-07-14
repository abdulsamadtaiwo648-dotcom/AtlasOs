namespace Atlas.Tools.Interfaces;

public interface ITool
{
    string Name { get; }

    bool CanHandle(string input);

    string Execute(string input);
}