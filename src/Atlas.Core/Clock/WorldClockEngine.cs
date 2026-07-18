namespace Atlas.Core.Clock;

public class WorldClockEngine
{
    public DateTime GetTime(string timeZoneId)
    {
        TimeZoneInfo zone =
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            zone);
    }
    public DateTime GetTimeForLocation(string locationName)
    {
        // Define a simple mapping of common cities/countries to Windows Time Zone IDs
        var locationMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "london", "GMT Standard Time" },
            { "uk", "GMT Standard Time" },
            { "lagos", "W. Central Africa Standard Time" },
            { "nigeria", "W. Central Africa Standard Time" },
            { "tokyo", "Tokyo Standard Time" },
            { "japan", "Tokyo Standard Time" },
            { "new york", "Eastern Standard Time" },
            { "ny", "Eastern Standard Time" },
            { "usa", "Eastern Standard Time" },
            { "paris", "Romance Standard Time" },
            { "france", "Romance Standard Time" },
            { "berlin", "W. Europe Standard Time" },
            { "germany", "W. Europe Standard Time" },
            { "dubai", "Arabian Standard Time" },
            { "uae", "Arabian Standard Time" },
            { "sydney", "AUS Eastern Standard Time" },
            { "australia", "AUS Eastern Standard Time" },
            { "toronto", "Eastern Standard Time" },
            { "canada", "Eastern Standard Time" },
            { "mumbai", "India Standard Time" },
            { "india", "India Standard Time" },
            { "moscow", "Russian Standard Time" },
            { "russia", "Russian Standard Time" }
        };

        string timeZoneId;
        if (locationMappings.TryGetValue(locationName, out var mappedId))
        {
            timeZoneId = mappedId;
        }
        else
        {
            // If the user happens to pass an exact TimeZone ID, we can try to use it
            timeZoneId = locationName;
        }

        try
        {
            return GetTime(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException($"Could not find time zone information for location: {locationName}");
        }
    }

    public DateTime London()
    {
        return GetTime("GMT Standard Time");
    }

    public DateTime Lagos()
    {
        return GetTime("W. Central Africa Standard Time");
    }

    public DateTime Tokyo()
    {
        return GetTime("Tokyo Standard Time");
    }

    public DateTime NewYork()
    {
        return GetTime("Eastern Standard Time");
    }
}