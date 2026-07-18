using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;

namespace Atlas.Finance.Analysis.ICT;

/// <summary>Detects ICT kill zones, sessions, daily bias, and Judas swings.</summary>
public static class KillZoneDetector
{
    // ── Kill Zones (UTC times) ─────────────────────────────────────────
    public static string GetActiveKillZone(DateTime utcNow)
    {
        int hour = utcNow.Hour;
        int min  = utcNow.Minute;
        double timeDecimal = hour + min / 60.0;

        if (timeDecimal >= 2.0  && timeDecimal < 5.0)  return "London";
        if (timeDecimal >= 12.0 && timeDecimal < 15.0) return "New York AM";
        if (timeDecimal >= 14.0 && timeDecimal < 16.0) return "London Close";
        if (timeDecimal >= 20.0 || timeDecimal < 1.0)  return "Asian";
        return "None";
    }

    public static string GetCurrentSession(DateTime utcNow)
    {
        int hour = utcNow.Hour;
        // Overlap handling — London + New York overlap 13:00–17:00 UTC
        if (hour >= 13 && hour < 17) return "London/NY Overlap Session";
        if (hour >= 8  && hour < 17) return "London Session";
        if (hour >= 13 && hour < 22) return "New York Session";
        if (hour >= 0  && hour < 9)  return "Asian Session";
        return "Off Hours";
    }

    public static string GetDailyBias(List<Candle> candles, MarketStructure structure)
    {
        if (structure == null) return "Neutral";

        decimal currentPrice = candles.Last().Close;

        // Bias based on premium/discount
        if (structure.IsInDiscount) return "Bullish";
        if (structure.IsInPremium)  return "Bearish";

        // Bias based on trend
        return structure.CurrentTrend switch
        {
            "Uptrend"   => "Bullish",
            "Downtrend" => "Bearish",
            _           => "Neutral"
        };
    }

    public static string GetSessionHighLow(List<Candle> candles, DateTime utcNow)
    {
        string session = GetCurrentSession(utcNow);
        var sessionCandles = candles
            .Where(c => c.Timestamp.Date == utcNow.Date)
            .ToList();

        if (!sessionCandles.Any())
            return $"{session}: No intraday data available.";

        decimal high = sessionCandles.Max(c => c.High);
        decimal low  = sessionCandles.Min(c => c.Low);
        return $"{session}: High ${high:N4} | Low ${low:N4}";
    }
}

/// <summary>Detects Judas Swing patterns — fake moves before the real session move.</summary>
public static class JudasSwingDetector
{
    public static (bool isJudas, string direction) Detect(
        List<Candle> candles, string dailyBias, string activeKillZone)
    {
        if (candles.Count < 5 || activeKillZone == "None") return (false, "");

        var recent = candles.TakeLast(5).ToList();
        decimal firstMove = recent[1].Close - recent[0].Close;
        decimal laterMove = recent.Last().Close - recent[1].Close;

        // Judas: initial move opposite to bias, then reversal
        bool judas = false;
        string direction = "";

        if (dailyBias == "Bullish" && firstMove < 0 && laterMove > Math.Abs(firstMove) * 1.5m)
        {
            judas = true;
            direction = "Bullish"; // initial fake drop, real move is up
        }
        else if (dailyBias == "Bearish" && firstMove > 0 && Math.Abs(laterMove) > firstMove * 1.5m && laterMove < 0)
        {
            judas = true;
            direction = "Bearish"; // initial fake pump, real move is down
        }

        return (judas, direction);
    }
}

/// <summary>Calculates the Optimal Trade Entry (OTE) zone — 61.8%–79% Fibonacci retracement.</summary>
public static class OTECalculator
{
    public static (decimal low, decimal high, bool isInZone, string direction) Calculate(
        List<Candle> candles, decimal currentPrice)
    {
        if (candles.Count < 10) return (0, 0, false, "");

        // Find most recent major swing (displacement move)
        decimal swingHigh = candles.TakeLast(50).Max(c => c.High);
        decimal swingLow  = candles.TakeLast(50).Min(c => c.Low);
        decimal swingRange = swingHigh - swingLow;
        if (swingRange == 0) return (0, 0, false, "");

        // Determine direction by where last impulse ended
        int lastHighIdx = candles.TakeLast(50).ToList().FindLastIndex(c => c.High == swingHigh);
        int lastLowIdx  = candles.TakeLast(50).ToList().FindLastIndex(c => c.Low  == swingLow);

        string direction;
        decimal ote618, ote786;

        if (lastLowIdx > lastHighIdx)
        {
            // Impulse was upward (low happened before high) — OTE is in the retracement
            direction = "Bullish";
            ote618 = swingHigh - swingRange * 0.618m;
            ote786 = swingHigh - swingRange * 0.786m;
        }
        else
        {
            // Impulse was downward — OTE is in the retracement
            direction = "Bearish";
            ote618 = swingLow + swingRange * 0.618m;
            ote786 = swingLow + swingRange * 0.786m;
        }

        decimal zoneHigh = Math.Max(ote618, ote786);
        decimal zoneLow  = Math.Min(ote618, ote786);
        bool inZone      = currentPrice >= zoneLow && currentPrice <= zoneHigh;

        return (zoneLow, zoneHigh, inZone, direction);
    }
}

/// <summary>Detects SMT (Smart Money Tool) divergence between correlated assets.</summary>
public static class SMTDivergenceDetector
{
    public static (bool detected, string description) Detect(
        List<Candle> primaryCandles,
        List<Candle>? secondaryCandles,
        string primarySymbol,
        string secondarySymbol)
    {
        if (secondaryCandles == null || secondaryCandles.Count < 5 || primaryCandles.Count < 5)
        {
            // Single-asset internal divergence check (RSI-like)
            var recent = primaryCandles.TakeLast(10).ToList();
            decimal priceChange = recent.Last().Close - recent.First().Close;
            decimal volChange   = recent.Last().Volume - recent.First().Volume;
            if (priceChange > 0 && volChange < 0)
                return (true, $"{primarySymbol} price rising but volume declining — potential hidden bearish divergence.");
            if (priceChange < 0 && volChange > 0)
                return (true, $"{primarySymbol} price falling but volume rising — potential capitulation or accumulation.");
            return (false, "");
        }

        // Cross-asset SMT divergence
        int lookback = Math.Min(10, Math.Min(primaryCandles.Count, secondaryCandles.Count));
        var pRecent  = primaryCandles.TakeLast(lookback).ToList();
        var sRecent  = secondaryCandles.TakeLast(lookback).ToList();

        decimal pHigh1 = pRecent.Take(lookback / 2).Max(c => c.High);
        decimal pHigh2 = pRecent.Skip(lookback / 2).Max(c => c.High);
        decimal sHigh1 = sRecent.Take(lookback / 2).Max(c => c.High);
        decimal sHigh2 = sRecent.Skip(lookback / 2).Max(c => c.High);

        decimal pLow1  = pRecent.Take(lookback / 2).Min(c => c.Low);
        decimal pLow2  = pRecent.Skip(lookback / 2).Min(c => c.Low);
        decimal sLow1  = sRecent.Take(lookback / 2).Min(c => c.Low);
        decimal sLow2  = sRecent.Skip(lookback / 2).Min(c => c.Low);

        // Bearish SMT: primary makes higher high, secondary doesn't
        if (pHigh2 > pHigh1 && sHigh2 < sHigh1)
            return (true, $"Bearish SMT: {primarySymbol} made a higher high but {secondarySymbol} failed to confirm — institutional selling signal.");

        // Bullish SMT: primary makes lower low, secondary doesn't
        if (pLow2 < pLow1 && sLow2 > sLow1)
            return (true, $"Bullish SMT: {primarySymbol} made a lower low but {secondarySymbol} failed to confirm — institutional buying signal.");

        return (false, "");
    }
}
