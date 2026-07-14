using Atlas.Finance.Models;

namespace Atlas.Finance.Analysis;

public class PortfolioAnalyzer
{
    public PortfolioAnalysis Analyze(List<MarketSummary> portfolio)
    {
        PortfolioAnalysis result = new();

        if (portfolio == null || portfolio.Count == 0)
        {
            result.Recommendations.Add("Portfolio is empty.");
            return result;
        }

        result.TotalAssets = portfolio.Count;

        result.TotalMarketValue =
            portfolio.Sum(x => x.Price);

        result.AverageChange =
            portfolio.Average(x => x.ChangePercent);

        //----------------------------------------
        // Overall Trend
        //----------------------------------------

        if (result.AverageChange >= 5)
            result.OverallTrend = "Strong Bullish";
        else if (result.AverageChange >= 1)
            result.OverallTrend = "Bullish";
        else if (result.AverageChange <= -5)
            result.OverallTrend = "Strong Bearish";
        else if (result.AverageChange < 0)
            result.OverallTrend = "Bearish";
        else
            result.OverallTrend = "Sideways";

        //----------------------------------------
        // Best Performer
        //----------------------------------------

        MarketSummary best =
            portfolio.MaxBy(x => x.ChangePercent)!;

        result.BestPerformer =
            $"{best.Name} ({best.ChangePercent:F2}%)";

        //----------------------------------------
        // Worst Performer
        //----------------------------------------

        MarketSummary worst =
            portfolio.MinBy(x => x.ChangePercent)!;

        result.WorstPerformer =
            $"{worst.Name} ({worst.ChangePercent:F2}%)";

        //----------------------------------------
        // Highest Value Asset
        //----------------------------------------

        MarketSummary highest =
            portfolio.MaxBy(x => x.Price)!;

        result.HighestValueAsset =
            $"{highest.Name} (${highest.Price:F2})";

        //----------------------------------------
        // Diversification
        //----------------------------------------

        result.DiversificationScore =
            Math.Min(100, portfolio.Count * 20);

        //----------------------------------------
        // Portfolio Risk
        //----------------------------------------

        if (result.AverageChange <= -8)
            result.PortfolioRisk = "Very High";
        else if (result.AverageChange <= -4)
            result.PortfolioRisk = "High";
        else if (result.AverageChange <= -2)
            result.PortfolioRisk = "Medium";
        else
            result.PortfolioRisk = "Low";

        //----------------------------------------
        // Recommendations
        //----------------------------------------

        if (result.DiversificationScore < 60)
            result.Recommendations.Add(
                "Consider diversifying across more asset classes.");

        if (result.PortfolioRisk == "High" ||
            result.PortfolioRisk == "Very High")
            result.Recommendations.Add(
                "Reduce exposure to highly volatile assets.");

        if (result.AverageChange > 3)
            result.Recommendations.Add(
                "Portfolio momentum is positive.");

        if (result.AverageChange < -3)
            result.Recommendations.Add(
                "Review losing positions.");

        return result;
    }
}