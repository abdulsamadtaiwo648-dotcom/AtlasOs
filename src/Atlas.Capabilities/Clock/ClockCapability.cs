using Atlas.Capabilities.Interfaces;
using Atlas.Core.Clock;

namespace Atlas.Capabilities.Clock;

public class ClockCapability : ICapability
{
    private readonly IClockEngine _clock;

    public ClockCapability(IClockEngine clock)
    {
        _clock = clock;
    }

    public string Name => "Clock";

    public bool CanHandle(string input)
    {
        input = input.ToLower();

        return input.Contains("time") ||
               input.Contains("clock") ||
               input.Contains("date") ||
               input.Contains("today") ||
               input.Contains("day") ||
               input.Contains("month") ||
               input.Contains("year");
    }

    public Task<string> ExecuteAsync(string input)
    {
        input = input.ToLower();

        if (input.Contains("time"))
        {
            return Task.FromResult(
                $"The current time is {_clock.GetCurrentTime()}.");
        }

        if (input.Contains("date") || input.Contains("today"))
        {
            return Task.FromResult(
                $"Today is {_clock.GetCurrentDate()}.");
        }

        if (input.Contains("day"))
        {
            return Task.FromResult(
                $"Today is {_clock.Now:dddd}.");
        }

        if (input.Contains("month"))
        {
            return Task.FromResult(
                $"The current month is {_clock.Now:MMMM}.");
        }

        if (input.Contains("year"))
        {
            return Task.FromResult(
                $"The current year is {_clock.Now:yyyy}.");
        }

        return Task.FromResult($"""
Current Date and Time

Date : {_clock.GetCurrentDate()}

Time : {_clock.GetCurrentTime()}
""");
    }
}