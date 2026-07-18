using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;

namespace Atlas.Finance.Engines;

public class ForecastEngine : IForecastEngine
{
    public ForecastResult Predict(TradingReport report)
    {
        int bullishScore = 0;
        int bearishScore = 0;
        int neutralScore = 0;

        // Analyze RSI
        if (report.RSI > 0)
        {
            if (report.RSI < 30)       bullishScore += 3; // Oversold bounce
            else if (report.RSI > 70)  bearishScore += 3; // Overbought pullback
            else if (report.RSI < 45)  bearishScore += 1;
            else if (report.RSI > 55)  bullishScore += 1;
            else                       neutralScore += 1;
        }

        // Analyze MACD
        if (report.MACD != 0)
        {
            if (report.MACD > 0) bullishScore += 2;
            else                 bearishScore += 2;
        }

        // Price vs moving averages
        if (report.CurrentPrice > 0)
        {
            if (report.SMA50 > 0)
            {
                if (report.CurrentPrice > report.SMA50) bullishScore += 1;
                else                                    bearishScore += 1;
            }
            if (report.SMA200 > 0)
            {
                if (report.CurrentPrice > report.SMA200) bullishScore += 2;
                else                                     bearishScore += 2;
            }
            if (report.EMA20 > 0)
            {
                if (report.CurrentPrice > report.EMA20) bullishScore += 1;
                else                                    bearishScore += 1;
            }
        }

        // Daily bias / Trend
        if (!string.IsNullOrEmpty(report.DailyBias))
        {
            if (report.DailyBias.Equals("Bullish", StringComparison.OrdinalIgnoreCase))      bullishScore += 3;
            else if (report.DailyBias.Equals("Bearish", StringComparison.OrdinalIgnoreCase)) bearishScore += 3;
        }
        else if (!string.IsNullOrEmpty(report.Trend))
        {
            if (report.Trend.Contains("Bullish", StringComparison.OrdinalIgnoreCase))      bullishScore += 2;
            else if (report.Trend.Contains("Bearish", StringComparison.OrdinalIgnoreCase)) bearishScore += 2;
        }

        int totalScore = bullishScore + bearishScore + neutralScore;
        if (totalScore == 0)
        {
            return new ForecastResult
            {
                BullishProbability = 33,
                NeutralProbability = 34,
                BearishProbability = 33,
                Recommendation     = "Hold"
            };
        }

        int bullishProb = (int)Math.Round((double)bullishScore / totalScore * 100);
        int bearishProb = (int)Math.Round((double)bearishScore / totalScore * 100);
        int neutralProb = 100 - bullishProb - bearishProb;

        string recommendation = "Hold";
        if (bullishProb >= 70)      recommendation = "Strong Buy";
        else if (bullishProb >= 55) recommendation = "Buy";
        else if (bearishProb >= 70) recommendation = "Strong Sell";
        else if (bearishProb >= 55) recommendation = "Sell";

        return new ForecastResult
        {
            BullishProbability = bullishProb,
            NeutralProbability = neutralProb,
            BearishProbability = bearishProb,
            Recommendation     = recommendation
        };
    }
}