namespace Atlas.Core.Clock;

public interface IClockEngine
{
    DateTime Now { get; }

    DateTime UtcNow { get; }

    string GetCurrentTime();

    string GetCurrentDate();

    string GetCurrentDateTime();
}