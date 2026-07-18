using Atlas.Finance.Models;

namespace Atlas.Finance.Analysis.Fundamental;

/// <summary>
/// Helper analyzer to determine weekly bias, monthly bias, and session-specific high/low boundaries.
/// </summary>
public static class SessionAnalyzer
{
    public static string GetWeeklyBias(List<Candle> dailyCandles)
    {
        if (dailyCandles.Count < 5) return "Neutral";
        var last5 = dailyCandles.TakeLast(5).ToList();
        decimal open = last5.First().Open;
        decimal close = last5.Last().Close;
        decimal change = close - open;

        if (change > open * 0.005m) return "Bullish";
        if (change < -open * 0.005m) return "Bearish";
        return "Neutral";
    }

    public static string GetMonthlyBias(List<Candle> dailyCandles)
    {
        if (dailyCandles.Count < 20) return "Neutral";
        var last20 = dailyCandles.TakeLast(20).ToList();
        decimal open = last20.First().Open;
        decimal close = last20.Last().Close;
        decimal change = close - open;

        if (change > open * 0.015m) return "Bullish";
        if (change < -open * 0.015m) return "Bearish";
        return "Neutral";
    }

    public static (decimal sessionHigh, decimal sessionLow, string session) GetSessionHighLow(
        List<Candle> candles, DateTime utcNow)
    {
        // Guess active session
        int hour = utcNow.Hour;
        string session = "Off Hours";
        int startHour = 0;
        int endHour = 24;

        if (hour >= 0 && hour < 9)
        {
            session = "Asian Session";
            startHour = 0;
            endHour = 9;
        }
        else if (hour >= 8 && hour < 17)
        {
            session = "London Session";
            startHour = 8;
            endHour = 17;
        }
        else if (hour >= 13 && hour < 22)
        {
            session = "New York Session";
            startHour = 13;
            endHour = 22;
        }

        var sessionCandles = candles
            .Where(c => c.Timestamp.Hour >= startHour && c.Timestamp.Hour < endHour)
            .ToList();

        if (sessionCandles.Any())
        {
            return (sessionCandles.Max(c => c.High), sessionCandles.Min(c => c.Low), session);
        }

        return (candles.LastOrDefault()?.High ?? 0m, candles.LastOrDefault()?.Low ?? 0m, session);
    }
}
