using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;

namespace Atlas.Finance.Engines;

public class MarketEngine : IMarketEngine
{
    private readonly IFinanceProvider _provider;

    public MarketEngine(IFinanceProvider provider)
    {
        _provider = provider;
    }

    public async Task<MarketSummary> AnalyzeCryptoAsync(string symbol)
    {
        CryptoQuote quote = await _provider.GetCryptoPriceAsync(symbol);

        return new MarketSummary
        {
            Symbol = quote.Symbol,
            Name = quote.Name,
            Price = quote.Price,
            ChangePercent = quote.Change24Hours,
            Volume = quote.Volume,
            MarketCap = quote.MarketCap,
            Trend = DetectTrend(quote.Change24Hours),
            Recommendation = Recommendation(quote.Change24Hours),
            Confidence = Confidence(quote.Change24Hours)
        };
    }

    public async Task<MarketSummary> AnalyzeStockAsync(string symbol)
    {
        StockQuote quote = await _provider.GetStockPriceAsync(symbol);

        return new MarketSummary
        {
            Symbol = quote.Symbol,
            Name = quote.Name,
            Price = quote.Price,
            ChangePercent = quote.ChangePercent,
            Volume = quote.Volume,
            MarketCap = quote.MarketCap,
            Trend = DetectTrend(quote.ChangePercent),
            Recommendation = Recommendation(quote.ChangePercent),
            Confidence = Confidence(quote.ChangePercent)
        };
    }

    public async Task<MarketSummary> AnalyzeForexAsync(string pair)
    {
        StockQuote quote = await _provider.GetStockPriceAsync(pair);

        return new MarketSummary
        {
            Symbol = quote.Symbol,
            Name = quote.Symbol,
            Price = quote.Price,
            ChangePercent = quote.ChangePercent,
            Trend = DetectTrend(quote.ChangePercent),
            Recommendation = Recommendation(quote.ChangePercent),
            Confidence = Confidence(quote.ChangePercent)
        };
    }

    public async Task<MarketSummary> AnalyzeCommodityAsync(string symbol)
    {
        StockQuote quote = await _provider.GetStockPriceAsync(symbol);

        return new MarketSummary
        {
            Symbol = quote.Symbol,
            Name = quote.Name,
            Price = quote.Price,
            ChangePercent = quote.ChangePercent,
            Trend = DetectTrend(quote.ChangePercent),
            Recommendation = Recommendation(quote.ChangePercent),
            Confidence = Confidence(quote.ChangePercent)
        };
    }

    private static string DetectTrend(decimal change)
    {
        if (change > 5) return "Strong Bullish";
        if (change > 1) return "Bullish";
        if (change < -5) return "Strong Bearish";
        if (change < -1) return "Bearish";

        return "Sideways";
    }

    private static string Recommendation(decimal change)
    {
        if (change > 5) return "Buy";
        if (change > 1) return "Watch";
        if (change < -5) return "Avoid";
        if (change < -1) return "Possible Dip Buy";

        return "Hold";
    }

    private static int Confidence(decimal change)
    {
        change = Math.Abs(change);

        if (change >= 10) return 90;
        if (change >= 5) return 80;
        if (change >= 2) return 70;

        return 60;
    }

    public async Task<List<Atlas.Finance.Models.Analysis.HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days = 30)
    {
        return await _provider.GetHistoricalPricesAsync(symbol, days);
    }
}