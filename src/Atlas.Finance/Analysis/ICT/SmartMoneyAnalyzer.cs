using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;

namespace Atlas.Finance.Analysis.ICT;

/// <summary>Orchestrates all ICT/SMC analysis into a single SmartMoneyAnalysis output.</summary>
public static class SmartMoneyAnalyzer
{
    public static SmartMoneyAnalysis Analyze(
        List<Candle> candles,
        string symbol,
        string timeframe,
        List<Candle>? correlatedCandles = null,
        string? correlatedSymbol       = null)
    {
        var sma = new SmartMoneyAnalysis
        {
            Symbol      = symbol,
            Timeframe   = timeframe,
            GeneratedAt = DateTime.UtcNow
        };

        if (candles.Count < 5) return sma;

        decimal currentPrice = candles.Last().Close;
        DateTime utcNow      = DateTime.UtcNow;

        // 1. Market Structure
        sma.MarketStructure = MarketStructureAnalyzer.Analyze(candles);

        // 2. Order Blocks
        sma.OrderBlocks = OrderBlockDetector.Detect(candles);

        // 3. Fair Value Gaps
        sma.FairValueGaps = FairValueGapDetector.Detect(candles);

        // 4. Liquidity
        sma.LiquidityLevels = LiquidityAnalyzer.Analyze(candles);

        // 5. Kill zone / session
        sma.ActiveKillZone = KillZoneDetector.GetActiveKillZone(utcNow);

        // 6. Biases
        sma.DailyBias   = KillZoneDetector.GetDailyBias(candles, sma.MarketStructure);
        sma.WeeklyBias  = GetWeeklyBias(candles);
        sma.MonthlyBias = GetMonthlyBias(candles);

        // 7. Judas swing
        var (isJudas, judasDirn) = JudasSwingDetector.Detect(candles, sma.DailyBias, sma.ActiveKillZone);
        sma.IsJudasSwing   = isJudas;
        sma.JudasDirection = judasDirn;

        // 8. OTE
        var (oteLow, oteHigh, isInOTE, oteDir) = OTECalculator.Calculate(candles, currentPrice);
        sma.OTEZoneLow  = oteLow;
        sma.OTEZoneHigh = oteHigh;
        sma.IsInOTE     = isInOTE;

        // 9. SMT Divergence
        var (smtDetected, smtDesc) = SMTDivergenceDetector.Detect(
            candles, correlatedCandles, symbol, correlatedSymbol ?? "");
        sma.SMTDivergence  = smtDetected;
        sma.SMTDescription = smtDesc;

        // 10. Institutional behaviour flags
        bool bullishOBsPresent = sma.OrderBlocks.Any(ob => ob.Zone == "Bullish" && ob.IsValid);
        bool bearishOBsPresent = sma.OrderBlocks.Any(ob => ob.Zone == "Bearish" && ob.IsValid);
        bool sweptLevel        = sma.LiquidityLevels.Any(l => l.IsSwept);
        bool equalHighsPresent = sma.LiquidityLevels.Any(l => l.Type == "Equal Highs" && !l.IsSwept);
        bool equalLowsPresent  = sma.LiquidityLevels.Any(l => l.Type == "Equal Lows"  && !l.IsSwept);

        sma.InstitutionalAccumulation = bullishOBsPresent && sma.MarketStructure.IsInDiscount;
        sma.InstitutionalDistribution = bearishOBsPresent && sma.MarketStructure.IsInPremium;
        sma.StopHuntDetected          = sweptLevel;
        sma.StopHuntDirection         = sweptLevel
            ? (sma.LiquidityLevels.First(l => l.IsSwept).Type.Contains("High") ? "Upward" : "Downward")
            : "";
        sma.EngineeredLiquidity  = equalHighsPresent || equalLowsPresent;
        sma.RetailTrapDetected   = sweptLevel && sma.LiquidityLevels.Any(l => l.IsSwept && l.Strength < 40);

        // 11. Market phase
        sma.Rebalancing = sma.FairValueGaps.Any(f => f.IsPartiallyFilled);
        sma.Retracement = sma.IsInOTE;
        sma.Expansion   = candles.Count >= 3 &&
            Math.Abs(candles.Last().Close - candles[^2].Close) >
            candles.TakeLast(20).Select(c => Math.Abs(c.Close - c.Open)).Average() * 2m;

        // 12. Build summary and reasons
        var reasons = new System.Text.StringBuilder();
        sma.Reasons = new List<string>();

        if (sma.MarketStructure != null)
        {
            sma.Reasons.Add($"Market structure: {sma.MarketStructure.CurrentTrend} | Internal: {sma.MarketStructure.InternalTrend}");
            sma.Reasons.Add($"Daily bias: {sma.DailyBias} | Weekly: {sma.WeeklyBias} | Monthly: {sma.MonthlyBias}");
            if (sma.MarketStructure.IsInPremium)  sma.Reasons.Add("Price is in PREMIUM zone — institutional selling area.");
            if (sma.MarketStructure.IsInDiscount) sma.Reasons.Add("Price is in DISCOUNT zone — institutional buying area.");
        }

        if (sma.OrderBlocks.Any(ob => ob.IsValid))
            sma.Reasons.Add($"{sma.OrderBlocks.Count(ob => ob.IsValid)} active order blocks detected.");
        if (sma.FairValueGaps.Any(f => !f.IsFilled))
            sma.Reasons.Add($"{sma.FairValueGaps.Count(f => !f.IsFilled)} open FVGs — price may return to fill these imbalances.");
        if (sma.InstitutionalAccumulation) sma.Reasons.Add("Institutional accumulation signals: bullish OBs in discount zone.");
        if (sma.InstitutionalDistribution) sma.Reasons.Add("Institutional distribution signals: bearish OBs in premium zone.");
        if (sma.StopHuntDetected)           sma.Reasons.Add($"Stop hunt detected ({sma.StopHuntDirection}) — retail stops triggered.");
        if (sma.IsJudasSwing)               sma.Reasons.Add($"Judas Swing ({sma.JudasDirection}): fake move detected — reversal expected.");
        if (sma.IsInOTE)                    sma.Reasons.Add($"Price in OTE zone ${sma.OTEZoneLow:N4}–${sma.OTEZoneHigh:N4} — institutional entry area.");
        if (sma.SMTDivergence)              sma.Reasons.Add($"SMT Divergence: {sma.SMTDescription}");

        sma.Summary = $"{symbol} — {sma.MarketStructure?.CurrentTrend ?? "Unknown"} | Bias: {sma.DailyBias} | " +
                      $"Kill Zone: {sma.ActiveKillZone} | OBs: {sma.OrderBlocks.Count} | FVGs: {sma.FairValueGaps.Count} | " +
                      (sma.InstitutionalAccumulation ? "ACCUMULATION " : "") +
                      (sma.InstitutionalDistribution ? "DISTRIBUTION " : "") +
                      (sma.StopHuntDetected ? "STOP HUNT DETECTED" : "");

        return sma;
    }

    private static string GetWeeklyBias(List<Candle> candles)
    {
        var week = candles.TakeLast(5).ToList();
        if (week.Count < 2) return "Neutral";
        decimal change = week.Last().Close - week.First().Open;
        return change > 0 ? "Bullish" : change < 0 ? "Bearish" : "Neutral";
    }

    private static string GetMonthlyBias(List<Candle> candles)
    {
        var month = candles.TakeLast(22).ToList();
        if (month.Count < 2) return "Neutral";
        decimal change = month.Last().Close - month.First().Open;
        return change > 0 ? "Bullish" : change < 0 ? "Bearish" : "Neutral";
    }
}
