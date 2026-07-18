using Atlas.Finance.Models;
using Atlas.Finance.Models.Technical;

namespace Atlas.Finance.Analysis.Technical;

/// <summary>Detects chart patterns (Head & Shoulders, Double Tops, Triangles, Flags, etc.).</summary>
public static class ChartPatternRecognizer
{
    public static List<ChartPattern> Recognize(List<Candle> candles)
    {
        var patterns = new List<ChartPattern>();
        if (candles.Count < 20) return patterns;

        var recent = candles.TakeLast(60).ToList();
        decimal currentPrice = recent.Last().Close;

        // Identify local swing highs and lows (5-bar pivot)
        var swingHighs = new List<(int idx, decimal price, DateTime ts)>();
        var swingLows  = new List<(int idx, decimal price, DateTime ts)>();
        int lb = 2;
        for (int i = lb; i < recent.Count - lb; i++)
        {
            bool isHigh = true, isLow = true;
            for (int j = i - lb; j <= i + lb; j++)
            {
                if (j == i) continue;
                if (recent[j].High >= recent[i].High) isHigh = false;
                if (recent[j].Low  <= recent[i].Low)  isLow  = false;
            }
            if (isHigh) swingHighs.Add((i, recent[i].High, recent[i].Timestamp));
            if (isLow)  swingLows.Add((i, recent[i].Low,  recent[i].Timestamp));
        }

        decimal periodHigh = recent.Max(c => c.High);
        decimal periodLow  = recent.Min(c => c.Low);
        decimal range      = periodHigh - periodLow;

        // ── Double Top ────────────────────────────────────────────────
        if (swingHighs.Count >= 2)
        {
            var last2Highs = swingHighs.TakeLast(2).ToList();
            decimal h1 = last2Highs[0].price, h2 = last2Highs[1].price;
            if (Math.Abs(h1 - h2) / Math.Max(h1, h2) < 0.015m)
            {
                decimal neckline = recent.Skip(last2Highs[0].idx).Take(last2Highs[1].idx - last2Highs[0].idx).Min(c => c.Low);
                decimal target   = neckline - (Math.Max(h1, h2) - neckline);
                patterns.Add(new ChartPattern
                {
                    Name = "Double Top", Type = "Bearish",
                    StartTimestamp = last2Highs[0].ts, EndTimestamp = last2Highs[1].ts,
                    BreakoutLevel = neckline, TargetPrice = target,
                    StopLoss = Math.Max(h1, h2) * 1.01m, Confidence = 72, IsConfirmed = currentPrice < neckline,
                    Description = $"Two equal highs near ${h1:N4}. Breakdown below neckline ${neckline:N4} targets ${target:N4}."
                });
            }
        }

        // ── Double Bottom ─────────────────────────────────────────────
        if (swingLows.Count >= 2)
        {
            var last2Lows = swingLows.TakeLast(2).ToList();
            decimal l1 = last2Lows[0].price, l2 = last2Lows[1].price;
            if (Math.Abs(l1 - l2) / Math.Max(l1, l2) < 0.015m)
            {
                decimal neckline = recent.Skip(last2Lows[0].idx).Take(last2Lows[1].idx - last2Lows[0].idx).Max(c => c.High);
                decimal target   = neckline + (neckline - Math.Min(l1, l2));
                patterns.Add(new ChartPattern
                {
                    Name = "Double Bottom", Type = "Bullish",
                    StartTimestamp = last2Lows[0].ts, EndTimestamp = last2Lows[1].ts,
                    BreakoutLevel = neckline, TargetPrice = target,
                    StopLoss = Math.Min(l1, l2) * 0.99m, Confidence = 72, IsConfirmed = currentPrice > neckline,
                    Description = $"Two equal lows near ${l1:N4}. Breakout above neckline ${neckline:N4} targets ${target:N4}."
                });
            }
        }

        // ── Head and Shoulders ────────────────────────────────────────
        if (swingHighs.Count >= 3)
        {
            var last3 = swingHighs.TakeLast(3).ToList();
            decimal lShoulder = last3[0].price, head = last3[1].price, rShoulder = last3[2].price;
            if (head > lShoulder && head > rShoulder && Math.Abs(lShoulder - rShoulder) / Math.Max(lShoulder, rShoulder) < 0.02m)
            {
                decimal neckline = recent.Skip(last3[0].idx).Take(last3[2].idx - last3[0].idx).Min(c => c.Low);
                decimal target   = neckline - (head - neckline);
                patterns.Add(new ChartPattern
                {
                    Name = "Head and Shoulders", Type = "Bearish",
                    StartTimestamp = last3[0].ts, EndTimestamp = last3[2].ts,
                    BreakoutLevel = neckline, TargetPrice = target,
                    StopLoss = head * 1.01m, Confidence = 80, IsConfirmed = currentPrice < neckline,
                    Description = $"Head ${head:N4} flanked by shoulders ${lShoulder:N4}/{rShoulder:N4}. Neckline ${neckline:N4}."
                });
            }
        }

        // ── Inverse H&S ───────────────────────────────────────────────
        if (swingLows.Count >= 3)
        {
            var last3 = swingLows.TakeLast(3).ToList();
            decimal lShoulder = last3[0].price, head = last3[1].price, rShoulder = last3[2].price;
            if (head < lShoulder && head < rShoulder && Math.Abs(lShoulder - rShoulder) / Math.Max(lShoulder, rShoulder) < 0.02m)
            {
                decimal neckline = recent.Skip(last3[0].idx).Take(last3[2].idx - last3[0].idx).Max(c => c.High);
                decimal target   = neckline + (neckline - head);
                patterns.Add(new ChartPattern
                {
                    Name = "Inverse Head and Shoulders", Type = "Bullish",
                    StartTimestamp = last3[0].ts, EndTimestamp = last3[2].ts,
                    BreakoutLevel = neckline, TargetPrice = target,
                    StopLoss = head * 0.99m, Confidence = 80, IsConfirmed = currentPrice > neckline,
                    Description = $"Inverse H&S: Head ${head:N4}, shoulders ${lShoulder:N4}/{rShoulder:N4}."
                });
            }
        }

        // ── Ascending Triangle ────────────────────────────────────────
        if (swingHighs.Count >= 2 && swingLows.Count >= 2)
        {
            var recentHighs = swingHighs.TakeLast(3).ToList();
            var recentLows  = swingLows.TakeLast(3).ToList();
            bool flatTop    = recentHighs.Count >= 2 && Math.Abs(recentHighs.First().price - recentHighs.Last().price) / recentHighs.Last().price < 0.01m;
            bool risingLows = recentLows.Count >= 2 && recentLows.Last().price > recentLows.First().price;
            if (flatTop && risingLows)
            {
                decimal resistance = recentHighs.Select(h => h.price).Average();
                patterns.Add(new ChartPattern
                {
                    Name = "Ascending Triangle", Type = "Bullish",
                    StartTimestamp = recent.First().Timestamp, EndTimestamp = recent.Last().Timestamp,
                    BreakoutLevel = resistance, TargetPrice = resistance + (resistance - recentLows.Min(l => l.price)),
                    StopLoss = recentLows.Last().price * 0.99m, Confidence = 70, IsConfirmed = currentPrice > resistance,
                    Description = $"Rising lows converging toward flat resistance at ${resistance:N4}."
                });
            }
        }

        // ── Descending Triangle ───────────────────────────────────────
        if (swingHighs.Count >= 2 && swingLows.Count >= 2)
        {
            var recentHighs = swingHighs.TakeLast(3).ToList();
            var recentLows  = swingLows.TakeLast(3).ToList();
            bool flatBottom = recentLows.Count >= 2 && Math.Abs(recentLows.First().price - recentLows.Last().price) / recentLows.Last().price < 0.01m;
            bool fallingHighs = recentHighs.Count >= 2 && recentHighs.Last().price < recentHighs.First().price;
            if (flatBottom && fallingHighs)
            {
                decimal support = recentLows.Select(l => l.price).Average();
                patterns.Add(new ChartPattern
                {
                    Name = "Descending Triangle", Type = "Bearish",
                    StartTimestamp = recent.First().Timestamp, EndTimestamp = recent.Last().Timestamp,
                    BreakoutLevel = support, TargetPrice = support - (recentHighs.Max(h => h.price) - support),
                    StopLoss = recentHighs.Last().price * 1.01m, Confidence = 70, IsConfirmed = currentPrice < support,
                    Description = $"Falling highs converging toward flat support at ${support:N4}."
                });
            }
        }

        // ── Rising Wedge ─────────────────────────────────────────────
        if (swingHighs.Count >= 2 && swingLows.Count >= 2)
        {
            var rHighs = swingHighs.TakeLast(2).ToList();
            var rLows  = swingLows.TakeLast(2).ToList();
            bool highsRising = rHighs[1].price > rHighs[0].price;
            bool lowsRising  = rLows[1].price  > rLows[0].price;
            bool converging  = (rHighs[1].price - rHighs[0].price) < (rLows[1].price - rLows[0].price) * 0.5m;
            if (highsRising && lowsRising && converging)
                patterns.Add(new ChartPattern
                {
                    Name = "Rising Wedge", Type = "Bearish",
                    StartTimestamp = rHighs[0].ts, EndTimestamp = rHighs[1].ts,
                    BreakoutLevel = rLows.Last().price, TargetPrice = rLows.First().price,
                    StopLoss = rHighs.Last().price * 1.01m, Confidence = 65,
                    Description = "Converging upward trendlines — bearish reversal pattern."
                });
        }

        // ── Falling Wedge ─────────────────────────────────────────────
        if (swingHighs.Count >= 2 && swingLows.Count >= 2)
        {
            var fHighs = swingHighs.TakeLast(2).ToList();
            var fLows  = swingLows.TakeLast(2).ToList();
            bool highsFalling = fHighs[1].price < fHighs[0].price;
            bool lowsFalling  = fLows[1].price  < fLows[0].price;
            bool converging   = Math.Abs(fHighs[0].price - fHighs[1].price) < Math.Abs(fLows[0].price - fLows[1].price);
            if (highsFalling && lowsFalling && converging)
                patterns.Add(new ChartPattern
                {
                    Name = "Falling Wedge", Type = "Bullish",
                    StartTimestamp = fHighs[0].ts, EndTimestamp = fLows[1].ts,
                    BreakoutLevel = fHighs.Last().price, TargetPrice = fHighs.First().price,
                    StopLoss = fLows.Last().price * 0.99m, Confidence = 65,
                    Description = "Converging downward trendlines — bullish reversal pattern."
                });
        }

        // ── Bullish Flag ──────────────────────────────────────────────
        if (recent.Count >= 15)
        {
            var pole = recent.Take(10).ToList();
            var flag = recent.Skip(10).ToList();
            decimal poleMove = pole.Last().Close - pole.First().Close;
            decimal flagRange = flag.Max(c => c.High) - flag.Min(c => c.Low);
            bool strongPole  = poleMove > flagRange * 3m && poleMove > 0;
            bool smallFlag   = flagRange < poleMove * 0.5m;
            if (strongPole && smallFlag)
                patterns.Add(new ChartPattern
                {
                    Name = "Bullish Flag", Type = "Bullish",
                    StartTimestamp = recent.First().Timestamp, EndTimestamp = recent.Last().Timestamp,
                    BreakoutLevel = flag.Max(c => c.High),
                    TargetPrice   = flag.Max(c => c.High) + poleMove,
                    StopLoss      = flag.Min(c => c.Low) * 0.99m, Confidence = 68,
                    Description   = "Strong upward pole followed by tight consolidation flag."
                });
        }

        // ── Breakout ──────────────────────────────────────────────────
        decimal resistance20 = recent.SkipLast(2).Max(c => c.High);
        if (currentPrice > resistance20 * 1.01m)
            patterns.Add(new ChartPattern
            {
                Name = "Breakout", Type = "Bullish",
                StartTimestamp = recent.First().Timestamp, EndTimestamp = recent.Last().Timestamp,
                BreakoutLevel = resistance20, TargetPrice = resistance20 + range * 0.5m,
                StopLoss = resistance20 * 0.99m, Confidence = 75, IsConfirmed = true,
                Description = $"Price broke above ${resistance20:N4} resistance with momentum."
            });

        // ── Breakdown ─────────────────────────────────────────────────
        decimal support20 = recent.SkipLast(2).Min(c => c.Low);
        if (currentPrice < support20 * 0.99m)
            patterns.Add(new ChartPattern
            {
                Name = "Breakdown", Type = "Bearish",
                StartTimestamp = recent.First().Timestamp, EndTimestamp = recent.Last().Timestamp,
                BreakoutLevel = support20, TargetPrice = support20 - range * 0.5m,
                StopLoss = support20 * 1.01m, Confidence = 75, IsConfirmed = true,
                Description = $"Price broke below ${support20:N4} support — bearish momentum."
            });

        return patterns;
    }
}
