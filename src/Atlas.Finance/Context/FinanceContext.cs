using Atlas.Finance.Models;

namespace Atlas.Finance.Context;

public class FinanceContext
{
    public List<MarketSummary> Cryptocurrencies { get; set; } = new();

    public List<MarketSummary> Stocks { get; set; } = new();

    public List<MarketSummary> Forex { get; set; } = new();

    public List<MarketSummary> Commodities { get; set; } = new();

    public EconomicSummary Economy { get; set; } = new();
}