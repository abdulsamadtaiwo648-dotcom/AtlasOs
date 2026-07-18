using System.Globalization;

namespace Atlas.Core.Clock;

public class ClockEngine : IClockEngine
{
    public DateTime Now => DateTime.Now;

    public DateTime UtcNow => DateTime.UtcNow;

    public string GetCurrentTime()
    {
        return Now.ToString(
            "hh:mm:ss tt",
            CultureInfo.InvariantCulture);
    }

    public string GetCurrentDate()
    {
        return Now.ToString(
            "dddd, dd MMMM yyyy",
            CultureInfo.InvariantCulture);
    }

    public string GetCurrentDateTime()
    {
        return Now.ToString(
            "dddd, dd MMMM yyyy  hh:mm:ss tt",
            CultureInfo.InvariantCulture);
    }
}