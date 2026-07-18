using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;

namespace Atlas.Finance.Analysis.ICT;

/// <summary>Identifies liquidity pools, equal highs/lows, and stop-hunt zones.</summary>
public static class LiquidityAnalyzer
{
    public static List<LiquidityLevel> Analyze(List<Candle> candles)
    {
        var result = new List<LiquidityLevel>();
        if (candles.Count < 5) return result;

        decimal currentPrice = candles.Last().Close;
        decimal tolerance    = 0.0015m; // 0.15%

        // ── Swing highs as BSL (Buy-Side Liquidity) ────────────────────
        var swingHighs = new List<(decimal price, DateTime ts)>();
        var swingLows  = new List<(decimal price, DateTime ts)>();

        for (int i = 2; i < candles.Count - 2; i++)
        {
            if (candles[i].High > candles[i-1].High && candles[i].High > candles[i+1].High
                && candles[i].High > candles[i-2].High && candles[i].High > candles[i+2].High)
                swingHighs.Add((candles[i].High, candles[i].Timestamp));

            if (candles[i].Low < candles[i-1].Low && candles[i].Low < candles[i+1].Low
                && candles[i].Low < candles[i-2].Low && candles[i].Low < candles[i+2].Low)
                swingLows.Add((candles[i].Low, candles[i].Timestamp));
        }

        // ── Equal Highs (BSL) ─────────────────────────────────────────
        for (int i = 0; i < swingHighs.Count - 1; i++)
            for (int j = i + 1; j < swingHighs.Count; j++)
            {
                decimal h1 = swingHighs[i].price, h2 = swingHighs[j].price;
                if (Math.Abs(h1 - h2) / Math.Max(h1, h2) <= tolerance)
                {
                    decimal level = (h1 + h2) / 2m;
                    bool swept    = currentPrice > level * 1.001m;
                    result.Add(new LiquidityLevel
                    {
                        Type      = "Equal Highs",
                        Price     = level,
                        High      = level * 1.001m,
                        Low       = level * 0.999m,
                        Timestamp = swingHighs[j].ts,
                        IsSwept   = swept,
                        Strength  = swept ? 30 : 80,
                        Symbol    = candles.Last().Symbol,
                        Description = $"Equal Highs (BSL) @ ${level:N4} — {(swept ? "SWEPT" : "INTACT liquidity pool above market")}."
                    });
                }
            }

        // ── Equal Lows (SSL) ──────────────────────────────────────────
        for (int i = 0; i < swingLows.Count - 1; i++)
            for (int j = i + 1; j < swingLows.Count; j++)
            {
                decimal l1 = swingLows[i].price, l2 = swingLows[j].price;
                if (Math.Abs(l1 - l2) / Math.Max(l1, l2) <= tolerance)
                {
                    decimal level = (l1 + l2) / 2m;
                    bool swept    = currentPrice < level * 0.999m;
                    result.Add(new LiquidityLevel
                    {
                        Type      = "Equal Lows",
                        Price     = level,
                        High      = level * 1.001m,
                        Low       = level * 0.999m,
                        Timestamp = swingLows[j].ts,
                        IsSwept   = swept,
                        Strength  = swept ? 30 : 80,
                        Symbol    = candles.Last().Symbol,
                        Description = $"Equal Lows (SSL) @ ${level:N4} — {(swept ? "SWEPT" : "INTACT sell-side liquidity below market")}."
                    });
                }
            }

        // ── Individual swing highs as BSL targets ─────────────────────
        foreach (var (price, ts) in swingHighs.TakeLast(5))
        {
            bool swept = currentPrice > price * 1.002m;
            result.Add(new LiquidityLevel
            {
                Type      = swept ? "Stop Hunt Zone" : "Buy-Side Liquidity",
                Price     = price,
                High      = price * 1.002m,
                Low       = price * 0.998m,
                Timestamp = ts,
                IsSwept   = swept,
                Strength  = 60,
                Symbol    = candles.Last().Symbol,
                Description = $"{(swept ? "Stop hunt" : "BSL")} @ ${price:N4} — {(swept ? "stops above this level were triggered." : "resting buy-stops above this swing high.")}."
            });
        }

        // ── Individual swing lows as SSL targets ──────────────────────
        foreach (var (price, ts) in swingLows.TakeLast(5))
        {
            bool swept = currentPrice < price * 0.998m;
            result.Add(new LiquidityLevel
            {
                Type      = swept ? "Stop Hunt Zone" : "Sell-Side Liquidity",
                Price     = price,
                High      = price * 1.002m,
                Low       = price * 0.998m,
                Timestamp = ts,
                IsSwept   = swept,
                Strength  = 60,
                Symbol    = candles.Last().Symbol,
                Description = $"{(swept ? "Stop hunt" : "SSL")} @ ${price:N4} — {(swept ? "stops below this level were triggered." : "resting sell-stops below this swing low.")}."
            });
        }

        return result
            .OrderByDescending(l => l.Strength)
            .DistinctBy(l => Math.Round(l.Price, 4))
            .Take(12)
            .ToList();
    }
}
