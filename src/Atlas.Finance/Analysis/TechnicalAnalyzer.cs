using Atlas.Finance.Models;

namespace Atlas.Finance.Analysis;

public class TechnicalAnalyzer
{
    public TechnicalAnalysis Analyze(MarketSummary market)
    {
        TechnicalAnalysis analysis = new();

        analysis.Symbol = market.Symbol;

        analysis.EntryPrice = market.Price;

        //------------------------------------
        // Trend
        //------------------------------------

        if (market.ChangePercent >= 5)
        {
            analysis.Trend = "Strong Bullish";
            analysis.Signal = "BUY";
            analysis.Strength = 90;
        }
        else if (market.ChangePercent >= 2)
        {
            analysis.Trend = "Bullish";
            analysis.Signal = "BUY";
            analysis.Strength = 75;
        }
        else if (market.ChangePercent >= 0)
        {
            analysis.Trend = "Neutral";
            analysis.Signal = "HOLD";
            analysis.Strength = 60;
        }
        else if (market.ChangePercent <= -5)
        {
            analysis.Trend = "Strong Bearish";
            analysis.Signal = "SELL";
            analysis.Strength = 90;
        }
        else
        {
            analysis.Trend = "Bearish";
            analysis.Signal = "SELL";
            analysis.Strength = 70;
        }

        //------------------------------------
        // Momentum
        //------------------------------------

        analysis.Momentum =
            Math.Abs(market.ChangePercent) >= 5
            ? "Strong"
            : "Weak";

        //------------------------------------
        // Support / Resistance
        //------------------------------------

        analysis.Support =
            market.Price * 0.97m;

        analysis.Resistance =
            market.Price * 1.03m;

        //------------------------------------
        // Entry / Exit
        //------------------------------------

        analysis.StopLoss =
            analysis.Support;

        analysis.TakeProfit =
            analysis.Resistance;

        //------------------------------------
        // Indicators
        //------------------------------------

        analysis.Indicators.Add($"Trend: {analysis.Trend}");
        analysis.Indicators.Add($"Momentum: {analysis.Momentum}");
        analysis.Indicators.Add($"24h Change: {market.ChangePercent:F2}%");

        //------------------------------------
        // Reasons
        //------------------------------------

        analysis.Reasons.Add(
            $"Price is {market.ChangePercent:F2}% over the last 24 hours.");

        analysis.Reasons.Add(
            $"Trend detected as {analysis.Trend}.");

        analysis.Reasons.Add(
            $"Suggested signal: {analysis.Signal}.");

        return analysis;
    }
}