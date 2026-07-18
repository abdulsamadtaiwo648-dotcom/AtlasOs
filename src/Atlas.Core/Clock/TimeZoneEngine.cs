namespace Atlas.Core.Clock;

public class TimeZoneEngine
{
    public IEnumerable<TimeZoneInfo> GetTimeZones()
    {
        return TimeZoneInfo.GetSystemTimeZones();
    }

    public DateTime ConvertTo(
        DateTime time,
        string timeZoneId)
    {
        TimeZoneInfo zone =
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        return TimeZoneInfo.ConvertTime(time, zone);
    }

    public DateTime LocalTime()
    {
        return DateTime.Now;
    }

    public DateTime UniversalTime()
    {
        return DateTime.UtcNow;
    }
}