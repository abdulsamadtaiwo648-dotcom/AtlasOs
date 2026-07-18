using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;

namespace Atlas.Finance.Analysis.Fundamental;

/// <summary>
/// Aggregates technical indicators, smart money structures, and news consensus to calculate institutional sentiment and overall model confidence.
/// </summary>
public static class SentimentAnalyzer
{
    public static string AnalyzeInstitutionalSentiment(
        SmartMoneyAnalysis smc,
        IndicatorSet indicators,
        NewsValidationReport news)
    {
        int score = 0;

        // SMC factors
        if (smc != null)
        {
            if (smc.InstitutionalAccumulation) score += 2;
            if (smc.InstitutionalDistribution) score -= 2;
            if (smc.DailyBias == "Bullish")      score += 1;
            if (smc.DailyBias == "Bearish")      score -= 1;
            if (smc.Expansion && smc.DailyBias == "Bullish") score += 1;
            if (smc.Expansion && smc.DailyBias == "Bearish") score -= 1;
        }

        // Technical indicators
        if (indicators != null)
        {
            if (indicators.GoldenCross) score += 2;
            if (indicators.DeathCross)  score -= 2;
            if (indicators.RSI < 30m)    score += 1; // Oversold buying pressure
            if (indicators.RSI > 70m)    score -= 1; // Overbought selling pressure
            if (indicators.CMFSignal == "Bullish") score += 1;
            if (indicators.CMFSignal == "Bearish") score -= 1;
            if (indicators.OBVTrend == "Rising")   score += 1;
            if (indicators.OBVTrend == "Falling")  score -= 1;
        }

        // News Consensus
        if (news != null)
        {
            if (news.ConsensusSentiment == "Bullish") score += 1;
            if (news.ConsensusSentiment == "Bearish") score -= 1;
        }

        return score switch
        {
            >= 4  => "Strong Institutional Accumulation / Buying Pressure",
            >= 2  => "Institutional Accumulation",
            >= 1  => "Mild Institutional Buying Bias",
            <= -4 => "Strong Institutional Distribution / Selling Pressure",
            <= -2 => "Institutional Distribution",
            <= -1 => "Mild Institutional Selling Bias",
            _     => "Neutral / Balanced Institutional Order Flow"
        };
    }

    public static int CalculateOverallConfidence(
        SmartMoneyAnalysis smc,
        IndicatorSet indicators,
        NewsValidationReport news)
    {
        int indicatorConfidence = 50;
        if (indicators != null)
        {
            int total = indicators.BullishIndicators + indicators.BearishIndicators + indicators.NeutralIndicators;
            int maxDominant = Math.Max(indicators.BullishIndicators, indicators.BearishIndicators);
            indicatorConfidence = total > 0 ? (maxDominant * 100 / total) : 50;
        }

        int newsConfidence = news?.ConfidenceScore ?? 50;
        int smcConfidence  = smc?.MarketStructure?.Events?.Count > 3 ? 85 : 65;

        // Weighted confidence: 50% indicators, 30% news, 20% SMC
        return (indicatorConfidence * 50 + newsConfidence * 30 + smcConfidence * 20) / 100;
    }
}
