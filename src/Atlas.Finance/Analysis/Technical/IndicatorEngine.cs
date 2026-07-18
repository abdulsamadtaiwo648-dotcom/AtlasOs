using Atlas.Finance.Models;
using Atlas.Finance.Models.Technical;

namespace Atlas.Finance.Analysis.Technical;

/// <summary>
/// Pure-math static indicator engine. All calculations are performed from OHLCV candle arrays.
/// No external libraries required.
/// </summary>
public static class IndicatorEngine
{
    // ══════════════════════════════════════════════════════════════════
    // RSI — Relative Strength Index (Wilder smoothing)
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateRSI(List<Candle> candles, int period = 14)
    {
        if (candles.Count < period + 1) return 50m;

        decimal avgGain = 0m, avgLoss = 0m;
        for (int i = 1; i <= period; i++)
        {
            decimal change = candles[i].Close - candles[i - 1].Close;
            if (change > 0) avgGain += change;
            else avgLoss += Math.Abs(change);
        }
        avgGain /= period;
        avgLoss /= period;

        for (int i = period + 1; i < candles.Count; i++)
        {
            decimal change = candles[i].Close - candles[i - 1].Close;
            if (change > 0)
            {
                avgGain = (avgGain * (period - 1) + change) / period;
                avgLoss = (avgLoss * (period - 1)) / period;
            }
            else
            {
                avgGain = (avgGain * (period - 1)) / period;
                avgLoss = (avgLoss * (period - 1) + Math.Abs(change)) / period;
            }
        }

        if (avgLoss == 0) return 100m;
        decimal rs = avgGain / avgLoss;
        return Math.Round(100m - (100m / (1m + rs)), 2);
    }

    // ══════════════════════════════════════════════════════════════════
    // EMA — Exponential Moving Average
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateEMA(List<decimal> values, int period)
    {
        if (values.Count < period) return values.Count > 0 ? values.Last() : 0m;
        decimal k = 2m / (period + 1m);
        decimal ema = values.Take(period).Average();
        for (int i = period; i < values.Count; i++)
            ema = values[i] * k + ema * (1m - k);
        return Math.Round(ema, 8);
    }

    public static decimal CalculateEMAFromCandles(List<Candle> candles, int period)
        => CalculateEMA(candles.Select(c => c.Close).ToList(), period);

    // ══════════════════════════════════════════════════════════════════
    // SMA
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateSMA(List<Candle> candles, int period)
    {
        if (candles.Count < period) return candles.Count > 0 ? candles.Select(c => c.Close).Average() : 0m;
        return candles.TakeLast(period).Select(c => c.Close).Average();
    }

    // ══════════════════════════════════════════════════════════════════
    // MACD
    // ══════════════════════════════════════════════════════════════════
    public static (decimal macd, decimal signal, decimal histogram) CalculateMACD(
        List<Candle> candles, int fast = 12, int slow = 26, int signalPeriod = 9)
    {
        if (candles.Count < slow + signalPeriod) return (0, 0, 0);

        var closes = candles.Select(c => c.Close).ToList();

        // Build EMA series
        var fastEMAs = new List<decimal>();
        var slowEMAs  = new List<decimal>();
        decimal kF = 2m / (fast + 1m);
        decimal kS = 2m / (slow + 1m);

        decimal emaF = closes.Take(fast).Average();
        decimal emaS = closes.Take(slow).Average();

        for (int i = fast; i < closes.Count; i++)
        {
            emaF = closes[i] * kF + emaF * (1m - kF);
            if (i >= slow - 1)
            {
                emaS = closes[i] * kS + emaS * (1m - kS);
                fastEMAs.Add(emaF);
                slowEMAs.Add(emaS);
            }
        }

        var macdLine = fastEMAs.Zip(slowEMAs, (f, s) => f - s).ToList();
        if (macdLine.Count == 0) return (0, 0, 0);

        decimal sig = CalculateEMA(macdLine, signalPeriod);
        decimal macdVal = macdLine.Last();
        return (Math.Round(macdVal, 8), Math.Round(sig, 8), Math.Round(macdVal - sig, 8));
    }

    // ══════════════════════════════════════════════════════════════════
    // ATR — Average True Range (Wilder)
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateATR(List<Candle> candles, int period = 14)
    {
        if (candles.Count < 2) return 0m;

        var trs = new List<decimal>();
        for (int i = 1; i < candles.Count; i++)
        {
            decimal tr = Math.Max(candles[i].High - candles[i].Low,
                         Math.Max(Math.Abs(candles[i].High - candles[i - 1].Close),
                                  Math.Abs(candles[i].Low  - candles[i - 1].Close)));
            trs.Add(tr);
        }
        if (trs.Count < period) return trs.Average();

        decimal atr = trs.Take(period).Average();
        for (int i = period; i < trs.Count; i++)
            atr = (atr * (period - 1) + trs[i]) / period;
        return Math.Round(atr, 8);
    }

    // ══════════════════════════════════════════════════════════════════
    // Bollinger Bands
    // ══════════════════════════════════════════════════════════════════
    public static (decimal upper, decimal middle, decimal lower, decimal width)
        CalculateBollingerBands(List<Candle> candles, int period = 20, decimal multiplier = 2m)
    {
        if (candles.Count < period) return (0, 0, 0, 0);
        var closes = candles.TakeLast(period).Select(c => c.Close).ToList();
        decimal sma = closes.Average();
        decimal variance = closes.Select(c => (c - sma) * (c - sma)).Average();
        decimal stdDev = (decimal)Math.Sqrt((double)variance);
        decimal upper  = sma + multiplier * stdDev;
        decimal lower  = sma - multiplier * stdDev;
        decimal width  = sma > 0 ? (upper - lower) / sma * 100m : 0m;
        return (Math.Round(upper, 8), Math.Round(sma, 8), Math.Round(lower, 8), Math.Round(width, 4));
    }

    // ══════════════════════════════════════════════════════════════════
    // ADX — Average Directional Index (Wilder)
    // ══════════════════════════════════════════════════════════════════
    public static (decimal adx, decimal plusDI, decimal minusDI)
        CalculateADX(List<Candle> candles, int period = 14)
    {
        if (candles.Count < period * 2) return (25m, 25m, 25m);

        var trs   = new List<decimal>();
        var plusDMs  = new List<decimal>();
        var minusDMs = new List<decimal>();

        for (int i = 1; i < candles.Count; i++)
        {
            decimal upMove   = candles[i].High - candles[i - 1].High;
            decimal downMove = candles[i - 1].Low - candles[i].Low;
            plusDMs.Add(upMove > downMove && upMove > 0 ? upMove : 0);
            minusDMs.Add(downMove > upMove && downMove > 0 ? downMove : 0);
            decimal tr = Math.Max(candles[i].High - candles[i].Low,
                         Math.Max(Math.Abs(candles[i].High - candles[i - 1].Close),
                                  Math.Abs(candles[i].Low  - candles[i - 1].Close)));
            trs.Add(tr);
        }

        // Wilder smoothing
        decimal smTR    = trs.Take(period).Sum();
        decimal smPlusDM  = plusDMs.Take(period).Sum();
        decimal smMinusDM = minusDMs.Take(period).Sum();

        var dxList = new List<decimal>();

        for (int i = period; i < trs.Count; i++)
        {
            smTR    = smTR    - smTR    / period + trs[i];
            smPlusDM  = smPlusDM  - smPlusDM  / period + plusDMs[i];
            smMinusDM = smMinusDM - smMinusDM / period + minusDMs[i];

            decimal pdi = smTR > 0 ? smPlusDM  / smTR * 100m : 0m;
            decimal mdi = smTR > 0 ? smMinusDM / smTR * 100m : 0m;
            decimal dx  = (pdi + mdi) > 0 ? Math.Abs(pdi - mdi) / (pdi + mdi) * 100m : 0m;
            dxList.Add(dx);
        }

        if (dxList.Count == 0) return (25m, 25m, 25m);

        decimal adx = dxList.Take(period).Average();
        for (int i = period; i < dxList.Count; i++)
            adx = (adx * (period - 1) + dxList[i]) / period;

        // Final DI values from last iteration
        decimal finalPDI = smTR > 0 ? smPlusDM  / smTR * 100m : 0m;
        decimal finalMDI = smTR > 0 ? smMinusDM / smTR * 100m : 0m;

        return (Math.Round(adx, 2), Math.Round(finalPDI, 2), Math.Round(finalMDI, 2));
    }

    // ══════════════════════════════════════════════════════════════════
    // Stochastic RSI
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateStochRSI(List<Candle> candles, int rsiPeriod = 14, int stochPeriod = 14)
    {
        if (candles.Count < rsiPeriod + stochPeriod + 1) return 50m;

        // Build RSI series
        var rsiSeries = new List<decimal>();
        for (int start = 0; start <= candles.Count - rsiPeriod - 1; start++)
            rsiSeries.Add(CalculateRSI(candles.Skip(start).Take(rsiPeriod + 1).ToList(), rsiPeriod));

        if (rsiSeries.Count < stochPeriod) return 50m;
        var window = rsiSeries.TakeLast(stochPeriod).ToList();
        decimal minRSI = window.Min();
        decimal maxRSI = window.Max();
        if (maxRSI - minRSI == 0) return 50m;
        return Math.Round((rsiSeries.Last() - minRSI) / (maxRSI - minRSI) * 100m, 2);
    }

    // ══════════════════════════════════════════════════════════════════
    // OBV — On-Balance Volume
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateOBV(List<Candle> candles)
    {
        if (candles.Count < 2) return 0m;
        decimal obv = 0m;
        for (int i = 1; i < candles.Count; i++)
        {
            if (candles[i].Close > candles[i - 1].Close)      obv += candles[i].Volume;
            else if (candles[i].Close < candles[i - 1].Close) obv -= candles[i].Volume;
        }
        return obv;
    }

    // ══════════════════════════════════════════════════════════════════
    // CMF — Chaikin Money Flow
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateCMF(List<Candle> candles, int period = 20)
    {
        if (candles.Count < period) return 0m;
        var window = candles.TakeLast(period).ToList();
        decimal sumMFV = 0m, sumVol = 0m;
        foreach (var c in window)
        {
            if (c.Range == 0) continue;
            decimal mfm = ((c.Close - c.Low) - (c.High - c.Close)) / c.Range;
            sumMFV += mfm * c.Volume;
            sumVol += c.Volume;
        }
        return sumVol == 0 ? 0m : Math.Round(sumMFV / sumVol, 6);
    }

    // ══════════════════════════════════════════════════════════════════
    // VWAP — Volume Weighted Average Price
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateVWAP(List<Candle> candles)
    {
        decimal sumTPV = 0m, sumVol = 0m;
        foreach (var c in candles)
        {
            sumTPV += c.TypicalPrice * c.Volume;
            sumVol += c.Volume;
        }
        return sumVol == 0 ? candles.LastOrDefault()?.Close ?? 0m : Math.Round(sumTPV / sumVol, 8);
    }

    // ══════════════════════════════════════════════════════════════════
    // Fibonacci Retracement
    // ══════════════════════════════════════════════════════════════════
    public static Dictionary<string, decimal> CalculateFibonacciRetracement(decimal high, decimal low)
    {
        decimal range = high - low;
        return new()
        {
            ["0"]     = Math.Round(high, 8),
            ["23.6"]  = Math.Round(high - range * 0.236m,  8),
            ["38.2"]  = Math.Round(high - range * 0.382m,  8),
            ["50"]    = Math.Round(high - range * 0.500m,  8),
            ["61.8"]  = Math.Round(high - range * 0.618m,  8),
            ["78.6"]  = Math.Round(high - range * 0.786m,  8),
            ["100"]   = Math.Round(low,  8),
            ["127.2"] = Math.Round(low  - range * 0.272m,  8),
            ["161.8"] = Math.Round(low  - range * 0.618m,  8),
        };
    }

    // ══════════════════════════════════════════════════════════════════
    // Pivot Points (Classic)
    // ══════════════════════════════════════════════════════════════════
    public static (decimal pp, decimal r1, decimal r2, decimal r3, decimal s1, decimal s2, decimal s3)
        CalculatePivotPoints(Candle c)
    {
        decimal pp = (c.High + c.Low + c.Close) / 3m;
        decimal r1 = 2m * pp - c.Low;
        decimal r2 = pp + (c.High - c.Low);
        decimal r3 = c.High + 2m * (pp - c.Low);
        decimal s1 = 2m * pp - c.High;
        decimal s2 = pp - (c.High - c.Low);
        decimal s3 = c.Low - 2m * (c.High - pp);
        return (pp, r1, r2, r3, s1, s2, s3);
    }

    // ══════════════════════════════════════════════════════════════════
    // Volume Analysis
    // ══════════════════════════════════════════════════════════════════
    public static (string signal, decimal avgVolume) AnalyzeVolume(List<Candle> candles, int period = 20)
    {
        if (candles.Count < 2) return ("Normal", 0);
        decimal avg = candles.TakeLast(Math.Min(period, candles.Count)).Select(c => c.Volume).Average();
        decimal cur = candles.Last().Volume;
        string sig = cur >= avg * 1.5m ? "High" : cur <= avg * 0.5m ? "Low" : "Normal";
        return (sig, Math.Round(avg, 0));
    }

    // ══════════════════════════════════════════════════════════════════
    // Historical Volatility (Annualised)
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateVolatility(List<Candle> candles, int period = 20)
    {
        if (candles.Count < period + 1) return 0m;
        var logReturns = new List<double>();
        for (int i = candles.Count - period; i < candles.Count; i++)
            if (candles[i - 1].Close > 0)
                logReturns.Add(Math.Log((double)(candles[i].Close / candles[i - 1].Close)));
        if (logReturns.Count < 2) return 0m;
        double mean = logReturns.Average();
        double variance = logReturns.Select(r => (r - mean) * (r - mean)).Average();
        double hv = Math.Sqrt(variance * 252) * 100;
        return Math.Round((decimal)hv, 2);
    }

    // ══════════════════════════════════════════════════════════════════
    // Momentum
    // ══════════════════════════════════════════════════════════════════
    public static decimal CalculateMomentum(List<Candle> candles, int period = 10)
    {
        if (candles.Count < period + 1) return 0m;
        return Math.Round(candles.Last().Close - candles[candles.Count - 1 - period].Close, 8);
    }

    // ══════════════════════════════════════════════════════════════════
    // BUILD FULL INDICATOR SET — the main method
    // ══════════════════════════════════════════════════════════════════
    public static IndicatorSet BuildIndicatorSet(List<Candle> candles, string timeframe = "1D")
    {
        var set = new IndicatorSet { Symbol = candles.FirstOrDefault()?.Symbol ?? "", Timeframe = timeframe };

        if (candles.Count < 5) return set;

        decimal price = candles.Last().Close;
        set.CurrentPrice = price;

        // ── RSI ──────────────────────────────────────────────────────
        set.RSI = CalculateRSI(candles);
        set.RSISignal = set.RSI < 30 ? "Oversold" : set.RSI > 70 ? "Overbought" : "Neutral";

        // ── MACD ─────────────────────────────────────────────────────
        var (macd, sig, hist) = CalculateMACD(candles);
        set.MACD = macd; set.MACDSignal = sig; set.MACDHistogram = hist;

        // Cross detection: check previous bar histogram sign
        if (candles.Count > 27)
        {
            var prev = candles.Take(candles.Count - 1).ToList();
            var (_, _, prevHist) = CalculateMACD(prev);
            if (prevHist < 0 && hist > 0)      set.MACDCross = "Bullish Cross";
            else if (prevHist > 0 && hist < 0) set.MACDCross = "Bearish Cross";
            else                               set.MACDCross = "None";
        }
        else set.MACDCross = "None";

        // ── Moving averages ───────────────────────────────────────────
        set.EMA9   = CalculateEMAFromCandles(candles, 9);
        set.EMA20  = CalculateEMAFromCandles(candles, 20);
        set.EMA50  = CalculateEMAFromCandles(candles, 50);
        set.EMA100 = CalculateEMAFromCandles(candles, 100);
        set.EMA200 = CalculateEMAFromCandles(candles, 200);
        set.SMA20  = CalculateSMA(candles, 20);
        set.SMA50  = CalculateSMA(candles, 50);
        set.SMA200 = CalculateSMA(candles, 200);

        set.PriceVsEMA50  = price >= set.EMA50  ? "Above" : "Below";
        set.PriceVsEMA200 = price >= set.EMA200 ? "Above" : "Below";
        set.GoldenCross = set.EMA50 > set.EMA200 && set.EMA50 > 0 && set.EMA200 > 0;
        set.DeathCross  = set.EMA50 < set.EMA200 && set.EMA50 > 0 && set.EMA200 > 0;

        // ── ATR ───────────────────────────────────────────────────────
        set.ATR = CalculateATR(candles);
        set.ATRPercent = price > 0 ? Math.Round(set.ATR / price * 100m, 4) : 0;

        // ── Bollinger Bands ───────────────────────────────────────────
        var (bUpper, bMid, bLower, bWidth) = CalculateBollingerBands(candles);
        set.BollingerUpper = bUpper; set.BollingerMiddle = bMid;
        set.BollingerLower = bLower; set.BollingerWidth  = bWidth;
        if (bWidth < 5m)              set.BollingerSignal = "Squeeze";
        else if (price >= bUpper * 0.99m) set.BollingerSignal = "Near Upper";
        else if (price <= bLower * 1.01m) set.BollingerSignal = "Near Lower";
        else                          set.BollingerSignal = "Middle";

        // ── ADX ───────────────────────────────────────────────────────
        var (adx, pdi, mdi) = CalculateADX(candles);
        set.ADX = adx; set.PlusDI = pdi; set.MinusDI = mdi;
        set.ADXSignal = adx < 20 ? "Weak" : adx < 40 ? "Moderate" : adx < 60 ? "Strong" : "Very Strong";

        // ── StochRSI ─────────────────────────────────────────────────
        set.StochRSI = CalculateStochRSI(candles);
        set.StochRSISignal = set.StochRSI < 20 ? "Oversold" : set.StochRSI > 80 ? "Overbought" : "Neutral";

        // ── OBV ───────────────────────────────────────────────────────
        set.OBV = CalculateOBV(candles);
        if (candles.Count >= 10)
        {
            decimal prevOBV = CalculateOBV(candles.Take(candles.Count - 10).ToList());
            set.OBVTrend = set.OBV > prevOBV * 1.01m ? "Rising" : set.OBV < prevOBV * 0.99m ? "Falling" : "Flat";
        }
        else set.OBVTrend = "Flat";

        // ── CMF ───────────────────────────────────────────────────────
        set.CMF = CalculateCMF(candles);
        set.CMFSignal = set.CMF > 0.05m ? "Bullish" : set.CMF < -0.05m ? "Bearish" : "Neutral";

        // ── VWAP ─────────────────────────────────────────────────────
        set.VWAP = CalculateVWAP(candles);
        set.PriceVsVWAP = price >= set.VWAP ? "Above" : "Below";

        // ── Fibonacci ─────────────────────────────────────────────────
        decimal periodHigh = candles.TakeLast(50).Max(c => c.High);
        decimal periodLow  = candles.TakeLast(50).Min(c => c.Low);
        set.FibLevels = CalculateFibonacciRetracement(periodHigh, periodLow);

        decimal fib618 = set.FibLevels.GetValueOrDefault("61.8");
        decimal fib786 = set.FibLevels.GetValueOrDefault("78.6");
        if (price >= Math.Min(fib618, fib786) && price <= Math.Max(fib618, fib786))
            set.OTEZone = "Price in OTE zone (61.8%-78.6%)";
        else
            set.OTEZone = "Outside OTE zone";

        // ── Pivot Points ─────────────────────────────────────────────
        Candle lastSession = candles.Last();
        var (pp, r1, r2, r3, s1, s2, s3) = CalculatePivotPoints(lastSession);
        set.PivotPoint = pp; set.R1 = r1; set.R2 = r2; set.R3 = r3;
        set.S1 = s1; set.S2 = s2; set.S3 = s3;

        // ── Volume ───────────────────────────────────────────────────
        set.Volume = candles.Last().Volume;
        var (volSig, avgVol) = AnalyzeVolume(candles);
        set.VolumeSignal = volSig; set.AvgVolume = avgVol;

        // ── Trend strength (via ADX) ──────────────────────────────────
        set.TrendStrength = adx < 20 ? "Weak" : adx < 40 ? "Moderate" : adx < 60 ? "Strong" : "Very Strong";

        // ── Momentum ─────────────────────────────────────────────────
        set.Momentum = CalculateMomentum(candles);
        set.MomentumSignal = set.Momentum > 0 ? "Positive" : set.Momentum < 0 ? "Negative" : "Flat";

        // ── Volatility ───────────────────────────────────────────────
        set.Volatility = CalculateVolatility(candles);
        set.VolatilitySignal = set.Volatility < 20 ? "Low" : set.Volatility < 50 ? "Moderate" : "High";

        // ── Overall signal (count bullish vs bearish) ─────────────────
        int bull = 0, bear = 0, neut = 0;

        void Score(bool bullCond, bool bearCond) { if (bullCond) bull++; else if (bearCond) bear++; else neut++; }

        Score(set.RSI < 40, set.RSI > 60);
        Score(set.MACDHistogram > 0, set.MACDHistogram < 0);
        Score(set.GoldenCross, set.DeathCross);
        Score(set.PriceVsEMA50 == "Above", set.PriceVsEMA50 == "Below");
        Score(set.PriceVsEMA200 == "Above", set.PriceVsEMA200 == "Below");
        Score(set.CMFSignal == "Bullish", set.CMFSignal == "Bearish");
        Score(set.OBVTrend == "Rising", set.OBVTrend == "Falling");
        Score(set.StochRSI < 30, set.StochRSI > 70);
        Score(set.PriceVsVWAP == "Above", set.PriceVsVWAP == "Below");
        Score(set.BollingerSignal == "Near Lower", set.BollingerSignal == "Near Upper");
        Score(pdi > mdi, mdi > pdi);

        set.BullishIndicators = bull;
        set.BearishIndicators = bear;
        set.NeutralIndicators = neut;

        int total = bull + bear + neut;
        if (total > 0)
        {
            int bullPct = bull * 100 / total;
            int bearPct = bear * 100 / total;
            set.OverallSignal = bullPct >= 70 ? "Strong Buy"
                              : bullPct >= 55 ? "Buy"
                              : bearPct >= 70 ? "Strong Sell"
                              : bearPct >= 55 ? "Sell"
                              : "Neutral";
        }
        else set.OverallSignal = "Neutral";

        return set;
    }
}
