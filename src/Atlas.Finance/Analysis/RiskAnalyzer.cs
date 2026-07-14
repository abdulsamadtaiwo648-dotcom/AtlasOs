using Atlas.Finance.Models;

namespace Atlas.Finance.Analysis;

public class RiskAnalyzer
{
    public RiskAnalysis Analyze(MarketSummary market)
    {
        RiskAnalysis result = new();

        result.Symbol = market.Symbol;

        decimal move = Math.Abs(market.ChangePercent);

        result.Volatility = move;

        //-------------------------------------------------
        // Risk Score
        //-------------------------------------------------

        if (move >= 15)
        {
            result.RiskScore = 95;
            result.RiskLevel = "Extreme";
        }
        else if (move >= 10)
        {
            result.RiskScore = 80;
            result.RiskLevel = "High";
        }
        else if (move >= 5)
        {
            result.RiskScore = 60;
            result.RiskLevel = "Medium";
        }
        else if (move >= 2)
        {
            result.RiskScore = 35;
            result.RiskLevel = "Low";
        }
        else
        {
            result.RiskScore = 15;
            result.RiskLevel = "Very Low";
        }

        //-------------------------------------------------
        // Position Size
        //-------------------------------------------------

        if (result.RiskScore >= 80)
            result.SuggestedPositionSize = 5;

        else if (result.RiskScore >= 60)
            result.SuggestedPositionSize = 10;

        else if (result.RiskScore >= 35)
            result.SuggestedPositionSize = 20;

        else
            result.SuggestedPositionSize = 30;

        //-------------------------------------------------
        // Stop Loss
        //-------------------------------------------------

        result.StopLossPercent =
            result.RiskLevel switch
            {
                "Extreme" => 10m,
                "High" => 7m,
                "Medium" => 5m,
                "Low" => 3m,
                _ => 2m
            };

        //-------------------------------------------------
        // Take Profit
        //-------------------------------------------------

        result.TakeProfitPercent =
            result.StopLossPercent * 2;

        result.RiskRewardRatio = 2.0m;

        //-------------------------------------------------
        // Reasons
        //-------------------------------------------------

        result.Reasons.Add(
            $"24h movement: {market.ChangePercent:F2}%");

        result.Reasons.Add(
            $"Volatility: {result.Volatility:F2}%");

        result.Reasons.Add(
            $"Risk Level: {result.RiskLevel}");

        result.Reasons.Add(
            $"Suggested Position Size: {result.SuggestedPositionSize}%");

        return result;
    }
}