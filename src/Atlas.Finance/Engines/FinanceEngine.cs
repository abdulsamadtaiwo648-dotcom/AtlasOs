using System.Text;
using Atlas.Finance.Analysis;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;

namespace Atlas.Finance.Engines;

public class FinanceEngine : IFinanceEngine
{
    private readonly IMarketEngine _market;
    private readonly IEconomyEngine _economy;

    private readonly RiskAnalyzer _risk;
    private readonly PortfolioAnalyzer _portfolio;
    private readonly TechnicalAnalyzer _technical;
    private readonly InvestmentAnalyzer _investment;

    public FinanceEngine(
        IMarketEngine market,
        IEconomyEngine economy,
        RiskAnalyzer risk,
        PortfolioAnalyzer portfolio,
        TechnicalAnalyzer technical,
        InvestmentAnalyzer investment)
    {
        _market = market;
        _economy = economy;

        _risk = risk;
        _portfolio = portfolio;
        _technical = technical;
        _investment = investment;
    }

    // Used by Atlas AI
    public async Task<string> BuildContextAsync(string input)
    {
        input = input.ToLower();

        StringBuilder context = new();

        context.AppendLine("LIVE MARKET DATA");
        context.AppendLine();

        if (input.Contains("crypto"))
        {
            MarketSummary btc =
                await _market.AnalyzeCryptoAsync("BTC");

            MarketSummary eth =
                await _market.AnalyzeCryptoAsync("ETH");

            context.AppendLine($"BTC : {btc.Price} ({btc.ChangePercent}%)");
            context.AppendLine($"ETH : {eth.Price} ({eth.ChangePercent}%)");
        }

        if (input.Contains("stock"))
        {
            MarketSummary apple =
                await _market.AnalyzeStockAsync("AAPL");

            context.AppendLine($"Apple : {apple.Price}");
        }

        if (input.Contains("forex"))
        {
            MarketSummary eurusd =
                await _market.AnalyzeForexAsync("EURUSD");

            context.AppendLine($"EUR/USD : {eurusd.Price}");
        }

        EconomicSummary economy =
            await _economy.GetSummaryAsync();

        context.AppendLine();
        context.AppendLine($"Interest Rate : {economy.InterestRate}%");
        context.AppendLine($"Inflation : {economy.InflationRate}%");

        return context.ToString();
    }

    // Used when Atlas handles finance directly
    public async Task<string> ProcessAsync(string input)
    {
        input = input.ToLower();

        MarketSummary report;

        if (input.Contains("bitcoin") || input.Contains("btc"))
        {
            report = await _market.AnalyzeCryptoAsync("BTC");
        }
        else if (input.Contains("ethereum") || input.Contains("eth"))
        {
            report = await _market.AnalyzeCryptoAsync("ETH");
        }
        else if (input.Contains("apple"))
        {
            report = await _market.AnalyzeStockAsync("AAPL");
        }
        else if (input.Contains("eurusd"))
        {
            report = await _market.AnalyzeForexAsync("EURUSD");
        }
        else
        {
            return await BuildContextAsync(input);
        }

        TechnicalAnalysis technical =
     _technical.Analyze(report);

        RiskAnalysis risk =
            _risk.Analyze(report);

        InvestmentAnalysis investment =
    _investment.Analyze(report);
        return $"""
{Format(report)}

----------------------------
Technical Analysis

Trend:
{technical.Trend}

Score:
{technical.Score}/100

Signal:
{technical.Signal}

----------------------------
Risk Analysis

Risk Level:
{risk.Level}

Risk Score:
{risk.Score}

Recommendation:
{risk.Recommendation}

----------------------------
Investment Analysis

Rating:
{investment.Rating}

Expected Return:
{investment.ExpectedReturn}%

Recommendation:
{investment.Recommendation}
""";
    }
    private static string Format(MarketSummary report)
    {
        return $"""
{report.Name} ({report.Symbol})

Price:
{report.Price}

24H Change:
{report.ChangePercent}%

Trend:
{report.Trend}

Recommendation:
{report.Recommendation}
""";
    }
}