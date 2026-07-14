using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface IFinanceProvider
{
    Task<CryptoQuote> GetCryptoPriceAsync(string symbol);

    Task<StockQuote> GetStockPriceAsync(string symbol);

    Task<EconomicSummary> GetEconomicSummaryAsync();
}