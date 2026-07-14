using Atlas.Capabilities.Interfaces;

namespace Atlas.Capabilities.Clock;

public class ClockCapability : ICapability
{
    public string Name => "Clock";

    public bool CanHandle(string input)
    {
        input = input.ToLower();

        return input.Contains("time")
            || input.Contains("date")
            || input.Contains("today");
    }

    public Task<string> ExecuteAsync(string input)
    {
        return Task.FromResult(
            $"Current time: {DateTime.Now:F}");
    }
}