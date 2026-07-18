using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;

namespace Atlas.Finance.Analysis.ICT;

/// <summary>
/// Analyzes market structure: BOS, CHoCH, MSS, swing highs/lows,
/// internal/external structure, and premium/discount zones.
/// </summary>
public static class MarketStructureAnalyzer
{
    public static MarketStructure Analyze(List<Candle> candles)
    {
        var ms = new MarketStructure { Symbol = candles.FirstOrDefault()?.Symbol ?? "" };
        if (candles.Count < 10) return ms;

        decimal currentPrice = candles.Last().Close;

        // ── Detect swing highs and lows (5-bar pivot) ─────────────────
        var swingHighs = new List<(int idx, decimal price, DateTime ts)>();
        var swingLows  = new List<(int idx, decimal price, DateTime ts)>();
        int lb = 2;

        for (int i = lb; i < candles.Count - lb; i++)
        {
            bool isHigh = true, isLow = true;
            for (int j = i - lb; j <= i + lb; j++)
            {
                if (j == i) continue;
                if (candles[j].High >= candles[i].High) isHigh = false;
                if (candles[j].Low  <= candles[i].Low)  isLow  = false;
            }
            if (isHigh) swingHighs.Add((i, candles[i].High, candles[i].Timestamp));
            if (isLow)  swingLows.Add((i, candles[i].Low,  candles[i].Timestamp));
        }

        if (swingHighs.Count == 0 || swingLows.Count == 0) return ms;

        // ── Last swing levels ─────────────────────────────────────────
        ms.LastSwingHigh     = swingHighs.Last().price;
        ms.LastSwingHighTime = swingHighs.Last().ts;
        ms.LastSwingLow      = swingLows.Last().price;
        ms.LastSwingLowTime  = swingLows.Last().ts;

        // ── Trend detection (Higher Highs/Lows or Lower Highs/Lows) ───
        int hhCount = 0, hlCount = 0, lhCount = 0, llCount = 0;
        for (int i = 1; i < swingHighs.Count; i++)
            if (swingHighs[i].price > swingHighs[i-1].price) hhCount++;
            else lhCount++;
        for (int i = 1; i < swingLows.Count; i++)
            if (swingLows[i].price > swingLows[i-1].price) hlCount++;
            else llCount++;

        if (hhCount >= lhCount && hlCount >= llCount)      ms.CurrentTrend = "Uptrend";
        else if (lhCount >= hhCount && llCount >= hlCount) ms.CurrentTrend = "Downtrend";
        else                                               ms.CurrentTrend = "Ranging";

        // ── Internal structure (last 20 candles) ──────────────────────
        if (candles.Count >= 20)
        {
            var inner = candles.TakeLast(20).ToList();
            var innerHigh = inner.Skip(2).Take(inner.Count - 4).Max(c => c.High);
            var innerLow  = inner.Skip(2).Take(inner.Count - 4).Min(c => c.Low);
            decimal innerClose = inner.Last().Close;
            ms.InternalTrend = innerClose > (innerHigh + innerLow) / 2m ? "Bullish Internal" : "Bearish Internal";
        }
        else ms.InternalTrend = ms.CurrentTrend;

        ms.ExternalTrend = ms.CurrentTrend;

        // ── BOS / CHoCH events ────────────────────────────────────────
        for (int i = 1; i < swingHighs.Count; i++)
        {
            decimal prevHigh = swingHighs[i-1].price;
            decimal currHigh = swingHighs[i].price;
            if (currHigh > prevHigh)
                ms.Events.Add(new StructureEvent
                {
                    Type = "BOS", Direction = "Bullish", Price = currHigh,
                    Timestamp = swingHighs[i].ts,
                    Description = $"Bullish BOS — new higher high at ${currHigh:N4}"
                });
        }
        for (int i = 1; i < swingLows.Count; i++)
        {
            decimal prevLow = swingLows[i-1].price;
            decimal currLow = swingLows[i].price;
            if (currLow < prevLow)
                ms.Events.Add(new StructureEvent
                {
                    Type = "BOS", Direction = "Bearish", Price = currLow,
                    Timestamp = swingLows[i].ts,
                    Description = $"Bearish BOS — new lower low at ${currLow:N4}"
                });
        }

        // CHoCH: In uptrend, price breaks below a swing low (bearish CHoCH)
        //        In downtrend, price breaks above a swing high (bullish CHoCH)
        if (ms.CurrentTrend == "Uptrend" && swingLows.Count >= 2)
        {
            decimal recentSwingLow = swingLows[^1].price;
            if (currentPrice < recentSwingLow)
                ms.Events.Add(new StructureEvent
                {
                    Type = "CHoCH", Direction = "Bearish", Price = recentSwingLow,
                    Timestamp = candles.Last().Timestamp,
                    Description = $"Bearish CHoCH — price broke below swing low ${recentSwingLow:N4} in uptrend. Potential reversal."
                });
        }
        if (ms.CurrentTrend == "Downtrend" && swingHighs.Count >= 2)
        {
            decimal recentSwingHigh = swingHighs[^1].price;
            if (currentPrice > recentSwingHigh)
                ms.Events.Add(new StructureEvent
                {
                    Type = "CHoCH", Direction = "Bullish", Price = recentSwingHigh,
                    Timestamp = candles.Last().Timestamp,
                    Description = $"Bullish CHoCH — price broke above swing high ${recentSwingHigh:N4} in downtrend. Potential reversal."
                });
        }

        // MSS (Market Structure Shift) — more sensitive, internal-level version
        if (candles.Count >= 10)
        {
            var last10 = candles.TakeLast(10).ToList();
            decimal l10High = last10.Take(5).Max(c => c.High);
            decimal l10Low  = last10.Take(5).Min(c => c.Low);
            if (last10.Last().Close > l10High)
                ms.Events.Add(new StructureEvent
                {
                    Type = "MSS", Direction = "Bullish", Price = l10High,
                    Timestamp = candles.Last().Timestamp,
                    Description = $"Bullish MSS — internal structure shift above ${l10High:N4}."
                });
            else if (last10.Last().Close < l10Low)
                ms.Events.Add(new StructureEvent
                {
                    Type = "MSS", Direction = "Bearish", Price = l10Low,
                    Timestamp = candles.Last().Timestamp,
                    Description = $"Bearish MSS — internal structure shift below ${l10Low:N4}."
                });
        }

        ms.Events = ms.Events.OrderByDescending(e => e.Timestamp).Take(10).ToList();

        // ── Premium / Discount / Equilibrium ──────────────────────────
        decimal swingRange = ms.LastSwingHigh - ms.LastSwingLow;
        if (swingRange > 0)
        {
            ms.PremiumZone     = ms.LastSwingLow + swingRange * 0.705m;
            ms.DiscountZone    = ms.LastSwingLow + swingRange * 0.295m;
            ms.EquilibriumZone = ms.LastSwingLow + swingRange * 0.500m;
            ms.IsInPremium     = currentPrice >= ms.PremiumZone;
            ms.IsInDiscount    = currentPrice <= ms.DiscountZone;
        }

        return ms;
    }
}
