namespace Atlas.Capabilities.Interfaces;

public interface ICapability
{
    string Name { get; }

    bool CanHandle(string input);

    Task<string> ExecuteAsync(string input);
}
