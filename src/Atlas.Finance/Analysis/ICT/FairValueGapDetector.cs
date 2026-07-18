using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;

namespace Atlas.Finance.Analysis.ICT;

/// <summary>Detects Fair Value Gaps (FVG) and Inverse FVGs from OHLCV candle arrays.</summary>
public static class FairValueGapDetector
{
    public static List<FairValueGap> Detect(List<Candle> candles)
    {
        var result = new List<FairValueGap>();
        if (candles.Count < 3) return result;

        decimal currentPrice = candles.Last().Close;

        for (int i = 2; i < candles.Count; i++)
        {
            var c0 = candles[i - 2];   // first candle
            var c1 = candles[i - 1];   // middle candle
            var c2 = candles[i];       // third candle

            // ── BULLISH FVG: gap between candle[0].high and candle[2].low ──
            if (c2.Low > c0.High)
            {
                decimal gapHigh = c2.Low;
                decimal gapLow  = c0.High;
                decimal midpoint = (gapHigh + gapLow) / 2m;
                bool isFilled   = currentPrice <= gapLow;
                bool isPartial  = !isFilled && currentPrice <= midpoint;
                result.Add(new FairValueGap
                {
                    Type             = isFilled ? "Inverse Bullish FVG" : "Bullish FVG",
                    High             = gapHigh,
                    Low              = gapLow,
                    Midpoint         = midpoint,
                    Timestamp        = c1.Timestamp,
                    IsFilled         = isFilled,
                    IsPartiallyFilled = isPartial,
                    Symbol           = c1.Symbol,
                    Description      = $"{(isFilled ? "Inverse " : "")}Bullish FVG: ${gapLow:N4}–${gapHigh:N4} " +
                                       $"({(gapHigh - gapLow) / gapLow * 100m:F2}% gap). " +
                                       (isFilled ? "Filled — now acts as resistance." : "Open — price may return to fill this imbalance.")
                });
            }

            // ── BEARISH FVG: gap between candle[0].low and candle[2].high ──
            if (c2.High < c0.Low)
            {
                decimal gapHigh = c0.Low;
                decimal gapLow  = c2.High;
                decimal midpoint = (gapHigh + gapLow) / 2m;
                bool isFilled   = currentPrice >= gapHigh;
                bool isPartial  = !isFilled && currentPrice >= midpoint;
                result.Add(new FairValueGap
                {
                    Type             = isFilled ? "Inverse Bearish FVG" : "Bearish FVG",
                    High             = gapHigh,
                    Low              = gapLow,
                    Midpoint         = midpoint,
                    Timestamp        = c1.Timestamp,
                    IsFilled         = isFilled,
                    IsPartiallyFilled = isPartial,
                    Symbol           = c1.Symbol,
                    Description      = $"{(isFilled ? "Inverse " : "")}Bearish FVG: ${gapLow:N4}–${gapHigh:N4} " +
                                       $"({(gapHigh - gapLow) / Math.Max(gapLow, 0.000001m) * 100m:F2}% gap). " +
                                       (isFilled ? "Filled — now acts as support." : "Open — price may return to fill this imbalance.")
                });
            }
        }

        // Return most recent 10 FVGs (open ones prioritised)
        return result
            .OrderByDescending(f => !f.IsFilled)
            .ThenByDescending(f => f.Timestamp)
            .Take(10)
            .ToList();
    }
}
