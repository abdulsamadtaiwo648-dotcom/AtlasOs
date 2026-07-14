using Atlas.Finance.Models;

namespace Atlas.Finance.Analysis;

public class InvestmentAnalyzer
{
    public InvestmentAnalysis Analyze(MarketSummary report)
{
    InvestmentAnalysis result = new();

    result.Rating =
        report.ChangePercent > 5 ? "Strong Buy" :
        report.ChangePercent > 1 ? "Buy" :
        report.ChangePercent < -5 ? "Sell" :
        "Hold";

    result.ExpectedReturn =
        report.ChangePercent * 2;

    result.Recommendation =
        result.Rating;

    return result;
}
}