namespace Atlas.Core.Clock;

public class CalendarEngine
{
    public int CurrentYear()
    {
        return DateTime.Now.Year;
    }

    public int CurrentMonth()
    {
        return DateTime.Now.Month;
    }

    public int CurrentDay()
    {
        return DateTime.Now.Day;
    }

    public DayOfWeek DayOfWeek()
    {
        return DateTime.Now.DayOfWeek;
    }

    public bool IsLeapYear()
    {
        return DateTime.IsLeapYear(DateTime.Now.Year);
    }

    public int DayOfYear()
    {
        return DateTime.Now.DayOfYear;
    }

    public int DaysInCurrentMonth()
    {
        return DateTime.DaysInMonth(
            DateTime.Now.Year,
            DateTime.Now.Month);
    }
}