using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;

namespace Atlas.Finance.Analysis.ICT;

/// <summary>Detects ICT Order Blocks, Breaker Blocks from OHLCV candle arrays.</summary>
public static class OrderBlockDetector
{
    public static List<OrderBlock> Detect(List<Candle> candles)
    {
        var result = new List<OrderBlock>();
        if (candles.Count < 5) return result;

        decimal currentPrice = candles.Last().Close;
        decimal avgBody = candles.TakeLast(20).Select(c => c.BodySize).Average();
        if (avgBody == 0) avgBody = 1m;

        for (int i = 1; i < candles.Count - 2; i++)
        {
            var ob = candles[i];
            // Look at the next 3 candles for impulse
            var nextCandles = candles.Skip(i + 1).Take(4).ToList();

            // ── BULLISH ORDER BLOCK: last bearish candle before bullish impulse ──
            if (!ob.IsBullish)
            {
                int bullCount = nextCandles.Count(c => c.IsBullish);
                decimal impulseMove = nextCandles.Count > 0 ? nextCandles.Last().Close - ob.Close : 0;
                if (bullCount >= 2 || (nextCandles.Count > 0 && nextCandles[0].BodySize > avgBody * 2m))
                {
                    bool mitigated = candles.Skip(i + 1).Any(c => c.Low <= ob.High && c.High >= ob.Low);
                    int strength = Math.Min(100, (int)(impulseMove / avgBody * 20));
                    result.Add(new OrderBlock
                    {
                        Zone        = "Bullish",
                        High        = ob.High,
                        Low         = ob.Low,
                        Open        = ob.Open,
                        Close       = ob.Close,
                        Timestamp   = ob.Timestamp,
                        IsMitigated = mitigated,
                        IsValid     = !mitigated,
                        Strength    = Math.Max(40, Math.Min(100, strength)),
                        Symbol      = ob.Symbol,
                        Description = $"Bullish OB at ${ob.Low:N4}–${ob.High:N4} ({ob.Timestamp:dd MMM}). " +
                                      (mitigated ? "MITIGATED." : "ACTIVE — institutional demand zone.")
                    });
                }
            }

            // ── BEARISH ORDER BLOCK: last bullish candle before bearish impulse ──
            if (ob.IsBullish)
            {
                int bearCount = nextCandles.Count(c => !c.IsBullish);
                decimal impulseMove = nextCandles.Count > 0 ? ob.Close - nextCandles.Last().Close : 0;
                if (bearCount >= 2 || (nextCandles.Count > 0 && nextCandles[0].BodySize > avgBody * 2m && !nextCandles[0].IsBullish))
                {
                    bool mitigated = candles.Skip(i + 1).Any(c => c.Low <= ob.High && c.High >= ob.Low);
                    int strength = Math.Min(100, (int)(impulseMove / avgBody * 20));
                    result.Add(new OrderBlock
                    {
                        Zone        = "Bearish",
                        High        = ob.High,
                        Low         = ob.Low,
                        Open        = ob.Open,
                        Close       = ob.Close,
                        Timestamp   = ob.Timestamp,
                        IsMitigated = mitigated,
                        IsValid     = !mitigated,
                        Strength    = Math.Max(40, Math.Min(100, strength)),
                        Symbol      = ob.Symbol,
                        Description = $"Bearish OB at ${ob.Low:N4}–${ob.High:N4} ({ob.Timestamp:dd MMM}). " +
                                      (mitigated ? "MITIGATED — may act as support now." : "ACTIVE — institutional supply zone.")
                    });
                }
            }
        }

        // Return only last 5 most recent, most relevant OBs
        return result
            .OrderByDescending(ob => ob.Timestamp)
            .Take(6)
            .ToList();
    }
}
