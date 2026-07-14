using Atlas.Finance.Interfaces;

namespace Atlas.Finance.Context;

public class FinanceContextBuilder
{
    private readonly IMarketEngine _market;
    private readonly IEconomyEngine _economy;

    public FinanceContextBuilder(
        IMarketEngine market,
        IEconomyEngine economy)
    {
        _market = market;
        _economy = economy;
    }

    public async Task<FinanceContext> BuildAsync(string input)
    {
        FinanceContext context = new();

        context.Cryptocurrencies.Add(
            await _market.AnalyzeCryptoAsync("BTC"));

        context.Cryptocurrencies.Add(
            await _market.AnalyzeCryptoAsync("ETH"));

        context.Stocks.Add(
            await _market.AnalyzeStockAsync("AAPL"));

        context.Stocks.Add(
            await _market.AnalyzeStockAsync("NVDA"));

        context.Forex.Add(
            await _market.AnalyzeForexAsync("EURUSD"));

        context.Commodities.Add(
            await _market.AnalyzeCommodityAsync("XAUUSD"));

        context.Economy =
            await _economy.GetSummaryAsync();

        return context;
    }
}