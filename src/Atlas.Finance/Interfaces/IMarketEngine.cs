using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface IMarketEngine
{
    Task<MarketSummary> AnalyzeCryptoAsync(string symbol);

    Task<MarketSummary> AnalyzeStockAsync(string symbol);

    Task<MarketSummary> AnalyzeForexAsync(string pair);

    Task<MarketSummary> AnalyzeCommodityAsync(string symbol);
}