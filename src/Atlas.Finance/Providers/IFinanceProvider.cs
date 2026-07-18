using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface IFinanceProvider
{
    Task<CryptoQuote> GetCryptoPriceAsync(string symbol);

    Task<StockQuote> GetStockPriceAsync(string symbol);

    Task<EconomicSummary> GetEconomicSummaryAsync();

    Task<List<Atlas.Finance.Models.Analysis.HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days = 30);

    Task<List<Candle>> GetOHLCVAsync(string symbol, string interval = "1d", int days = 100);

    Task<NewsAnalysis> GetNewsAsync(string asset);
}