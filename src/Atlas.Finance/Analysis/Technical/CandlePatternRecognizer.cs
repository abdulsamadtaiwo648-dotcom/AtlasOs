using Atlas.Finance.Models;
using Atlas.Finance.Models.Technical;

namespace Atlas.Finance.Analysis.Technical;

/// <summary>Detects candlestick patterns from OHLCV candle arrays.</summary>
public static class CandlePatternRecognizer
{
    public static List<CandlePattern> Recognize(List<Candle> candles)
    {
        var patterns = new List<CandlePattern>();
        if (candles.Count < 3) return patterns;

        var c  = candles[^1];  // last candle
        var c1 = candles[^2];  // second-to-last
        var c2 = candles.Count >= 3 ? candles[^3] : c1;

        // Average body / range for reference
        decimal avgBody  = candles.TakeLast(10).Select(x => x.BodySize).Average();
        decimal avgRange = candles.TakeLast(10).Select(x => x.Range).Average();

        // ── 1. Hammer ─────────────────────────────────────────────────
        if (c.Range > 0 && c.LowerWick >= c.BodySize * 2m && c.UpperWick <= c.BodySize * 0.5m && c.BodySize > 0)
            patterns.Add(new CandlePattern
            {
                Name = "Hammer", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 70, Description = "Long lower wick with small body — buyers rejected lower prices.",
                TradingImplication = "Potential reversal to the upside; watch for bullish confirmation next candle."
            });

        // ── 2. Shooting Star ─────────────────────────────────────────
        if (c.Range > 0 && c.UpperWick >= c.BodySize * 2m && c.LowerWick <= c.BodySize * 0.5m && c.BodySize > 0)
            patterns.Add(new CandlePattern
            {
                Name = "Shooting Star", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 70, Description = "Long upper wick with small body — sellers pushed price back down.",
                TradingImplication = "Potential reversal to the downside; look for bearish follow-through."
            });

        // ── 3. Doji ───────────────────────────────────────────────────
        if (c.Range > 0 && c.BodySize <= c.Range * 0.05m)
            patterns.Add(new CandlePattern
            {
                Name = "Doji", Type = "Neutral", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 55, Description = "Indecision candle — open and close nearly equal.",
                TradingImplication = "Market indecision; wait for direction confirmation."
            });

        // ── 4. Spinning Top ───────────────────────────────────────────
        if (c.Range > 0 && c.BodySize <= c.Range * 0.30m && c.UpperWick > c.BodySize && c.LowerWick > c.BodySize)
            patterns.Add(new CandlePattern
            {
                Name = "Spinning Top", Type = "Neutral", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 45, Description = "Small body with significant wicks on both sides.",
                TradingImplication = "Uncertainty in the market; neither bulls nor bears in control."
            });

        // ── 5. Marubozu (Bullish) ─────────────────────────────────────
        if (c.IsBullish && c.BodySize >= c.Range * 0.90m)
            patterns.Add(new CandlePattern
            {
                Name = "Bullish Marubozu", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 80, Description = "Full bullish candle with no wicks — pure buying pressure.",
                TradingImplication = "Strong bullish momentum; trend continuation likely."
            });

        // ── 6. Marubozu (Bearish) ─────────────────────────────────────
        if (!c.IsBullish && c.BodySize >= c.Range * 0.90m && c.BodySize > 0)
            patterns.Add(new CandlePattern
            {
                Name = "Bearish Marubozu", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 80, Description = "Full bearish candle with no wicks — pure selling pressure.",
                TradingImplication = "Strong bearish momentum; downtrend continuation likely."
            });

        // ── 7. Bullish Engulfing ──────────────────────────────────────
        if (c.IsBullish && !c1.IsBullish && c.Open < c1.Close && c.Close > c1.Open)
            patterns.Add(new CandlePattern
            {
                Name = "Bullish Engulfing", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 78, Description = "Large bullish candle fully engulfs the prior bearish candle.",
                TradingImplication = "Strong reversal signal; buyers have taken control."
            });

        // ── 8. Bearish Engulfing ──────────────────────────────────────
        if (!c.IsBullish && c1.IsBullish && c.Open > c1.Close && c.Close < c1.Open)
            patterns.Add(new CandlePattern
            {
                Name = "Bearish Engulfing", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 78, Description = "Large bearish candle fully engulfs the prior bullish candle.",
                TradingImplication = "Strong bearish reversal; sellers have overwhelmed buyers."
            });

        // ── 9. Bullish Harami ─────────────────────────────────────────
        if (c.IsBullish && !c1.IsBullish && c.Open > c1.Close && c.Close < c1.Open && c.BodySize < c1.BodySize)
            patterns.Add(new CandlePattern
            {
                Name = "Bullish Harami", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 60, Description = "Small bullish candle inside a large bearish candle.",
                TradingImplication = "Possible bullish reversal; momentum is slowing down."
            });

        // ── 10. Bearish Harami ────────────────────────────────────────
        if (!c.IsBullish && c1.IsBullish && c.Open < c1.Close && c.Close > c1.Open && c.BodySize < c1.BodySize)
            patterns.Add(new CandlePattern
            {
                Name = "Bearish Harami", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 60, Description = "Small bearish candle inside a large bullish candle.",
                TradingImplication = "Potential bearish reversal; upward momentum waning."
            });

        // ── 11. Inside Bar ────────────────────────────────────────────
        if (c.High < c1.High && c.Low > c1.Low)
            patterns.Add(new CandlePattern
            {
                Name = "Inside Bar", Type = "Neutral", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 50, Description = "Candle is completely contained within the previous candle's range.",
                TradingImplication = "Consolidation; a breakout is likely — watch the direction."
            });

        // ── 12. Outside Bar ───────────────────────────────────────────
        if (c.High > c1.High && c.Low < c1.Low)
            patterns.Add(new CandlePattern
            {
                Name = "Outside Bar", Type = c.IsBullish ? "Bullish" : "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 65, Description = "Candle engulfs the full range of the previous candle.",
                TradingImplication = "High volatility reversal signal; direction determined by close."
            });

        // ── 13. Morning Star ──────────────────────────────────────────
        if (candles.Count >= 3 && !c2.IsBullish && c2.BodySize > avgBody
            && c1.BodySize < avgBody * 0.5m
            && c.IsBullish && c.Close > (c2.Open + c2.Close) / 2m)
            patterns.Add(new CandlePattern
            {
                Name = "Morning Star", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 85, Description = "3-candle bullish reversal: bearish candle, small doji/star, strong bullish candle.",
                TradingImplication = "Strong bottom reversal signal; trend change to upside likely."
            });

        // ── 14. Evening Star ──────────────────────────────────────────
        if (candles.Count >= 3 && c2.IsBullish && c2.BodySize > avgBody
            && c1.BodySize < avgBody * 0.5m
            && !c.IsBullish && c.Close < (c2.Open + c2.Close) / 2m)
            patterns.Add(new CandlePattern
            {
                Name = "Evening Star", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 85, Description = "3-candle bearish reversal: bullish candle, small star, strong bearish candle.",
                TradingImplication = "Strong top reversal signal; trend change to downside likely."
            });

        // ── 15. Three White Soldiers ──────────────────────────────────
        if (candles.Count >= 3 && c.IsBullish && c1.IsBullish && c2.IsBullish
            && c.Close > c1.Close && c1.Close > c2.Close
            && c.BodySize > avgBody * 0.6m && c1.BodySize > avgBody * 0.6m)
            patterns.Add(new CandlePattern
            {
                Name = "Three White Soldiers", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 88, Description = "Three consecutive strong bullish candles each closing higher.",
                TradingImplication = "Powerful bullish momentum; strong uptrend continuation signal."
            });

        // ── 16. Three Black Crows ─────────────────────────────────────
        if (candles.Count >= 3 && !c.IsBullish && !c1.IsBullish && !c2.IsBullish
            && c.Close < c1.Close && c1.Close < c2.Close
            && c.BodySize > avgBody * 0.6m && c1.BodySize > avgBody * 0.6m)
            patterns.Add(new CandlePattern
            {
                Name = "Three Black Crows", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 88, Description = "Three consecutive strong bearish candles each closing lower.",
                TradingImplication = "Powerful bearish momentum; strong downtrend continuation signal."
            });

        // ── 17. Tweezer Top ───────────────────────────────────────────
        decimal highTol = c1.High * 0.001m;
        if (!c.IsBullish && Math.Abs(c.High - c1.High) <= highTol)
            patterns.Add(new CandlePattern
            {
                Name = "Tweezer Top", Type = "Bearish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 68, Description = "Two candles with matching highs — double rejection at resistance.",
                TradingImplication = "Bearish reversal; resistance is strong, expect downward move."
            });

        // ── 18. Tweezer Bottom ────────────────────────────────────────
        decimal lowTol = c1.Low * 0.001m;
        if (c.IsBullish && Math.Abs(c.Low - c1.Low) <= lowTol)
            patterns.Add(new CandlePattern
            {
                Name = "Tweezer Bottom", Type = "Bullish", Timestamp = c.Timestamp, Price = c.Close,
                Strength = 68, Description = "Two candles with matching lows — double support bounce.",
                TradingImplication = "Bullish reversal; support is strong, expect upward move."
            });

        return patterns;
    }
}
