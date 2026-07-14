namespace Atlas.Core.Interfaces;

public interface ICommand
{
    string Name { get; }

    string Description { get; }

    string Execute(string[] args);
}