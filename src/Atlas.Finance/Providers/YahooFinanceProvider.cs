using System.Text.Json;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Providers;

/// <summary>
/// Free, key-less data provider using public Yahoo Finance API chart and query endpoints.
/// </summary>
public class YahooFinanceProvider : IFinanceProvider
{
    private readonly HttpClient _http;

    public YahooFinanceProvider(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AtlasOS/1.0");
    }

    private static string MapSymbol(string symbol)
    {
        string sym = symbol.ToUpper().Trim();
        
        // Crypto
        if (sym is "BTC" or "ETH" or "SOL" or "BNB" or "XRP" or "DOGE" or "ADA" or "AVAX" or "DOT" or "LINK" or "LTC" or "MATIC" or "SHIB")
            return $"{sym}-USD";
        if (sym.EndsWith("-USD"))
            return sym;

        // Forex
        if (sym.Length == 6 && (sym.StartsWith("EUR") || sym.StartsWith("GBP") || sym.StartsWith("USD") || sym.StartsWith("AUD") || sym.StartsWith("JPY") || sym.StartsWith("CAD") || sym.StartsWith("CHF") || sym.StartsWith("NZD")))
            return $"{sym}=X";
        if (sym.EndsWith("/USD") || sym.EndsWith("/JPY") || sym.EndsWith("/GBP") || sym.EndsWith("/EUR"))
            return $"{sym.Replace("/", "")}=X";

        // Commodities
        if (sym is "GOLD" or "XAU") return "GC=F";
        if (sym is "SILVER" or "XAG") return "SI=F";
        if (sym is "OIL" or "WTI") return "CL=F";
        if (sym is "BRENT") return "BZ=F";

        // Indices
        if (sym is "SP500" or "S&P500" or "SPX") return "^GSPC";
        if (sym is "NASDAQ" or "COMP") return "^IXIC";
        if (sym is "DOW" or "DJI") return "^DJI";
        if (sym is "VIX") return "^VIX";
        if (sym is "DXY") return "DX-Y.NYB";

        return sym;
    }

    public async Task<StockQuote> GetStockPriceAsync(string symbol)
    {
        string mapped = MapSymbol(symbol);
        string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(mapped)}?interval=1d&range=1d";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var meta = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0]
            .GetProperty("meta");

        return new StockQuote
        {
            Symbol        = symbol.ToUpper(),
            Name          = meta.TryGetProperty("longName", out var ln) ? ln.GetString() ?? symbol : symbol,
            Price         = meta.TryGetProperty("regularMarketPrice", out var p) ? p.GetDecimal() : 0m,
            ChangePercent = meta.TryGetProperty("regularMarketChangePercent", out var cp) ? cp.GetDecimal() : 0m,
            Volume        = meta.TryGetProperty("regularMarketVolume", out var v) ? v.GetDecimal() : 0m,
            Open          = meta.TryGetProperty("regularMarketOpen", out var o) ? o.GetDecimal() : 0m,
            High          = meta.TryGetProperty("regularMarketDayHigh", out var h) ? h.GetDecimal() : 0m,
            Low           = meta.TryGetProperty("regularMarketDayLow", out var l) ? l.GetDecimal() : 0m,
            PreviousClose = meta.TryGetProperty("regularMarketPreviousClose", out var pc) ? pc.GetDecimal() : 0m,
            LastUpdated   = DateTime.UtcNow
        };
    }

    public async Task<CryptoQuote> GetCryptoPriceAsync(string symbol)
    {
        string mapped = MapSymbol(symbol);
        string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(mapped)}?interval=1d&range=1d";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var meta = doc.RootElement
            .GetProperty("chart")
            .GetProperty("result")[0]
            .GetProperty("meta");

        return new CryptoQuote
        {
            Symbol        = symbol.ToUpper(),
            Name          = meta.TryGetProperty("longName", out var ln) ? ln.GetString() ?? symbol : symbol,
            Price         = meta.TryGetProperty("regularMarketPrice", out var p) ? p.GetDecimal() : 0m,
            Change24Hours = meta.TryGetProperty("regularMarketChangePercent", out var cp) ? cp.GetDecimal() : 0m,
            Volume        = meta.TryGetProperty("regularMarketVolume", out var v) ? v.GetDecimal() : 0m,
            MarketCap     = meta.TryGetProperty("marketCap", out var mc) ? mc.GetDecimal() : 0m,
            Currency      = meta.TryGetProperty("currency", out var c) ? c.GetString() ?? "USD" : "USD",
            LastUpdated   = DateTime.UtcNow
        };
    }

    public Task<EconomicSummary> GetEconomicSummaryAsync()
    {
        return Task.FromResult(new EconomicSummary
        {
            Country          = "US",
            InterestRate     = 4.5m,
            InflationRate    = 2.5m,
            GdpGrowth        = 2.1m,
            UnemploymentRate = 3.8m,
            Outlook          = "Normal"
        });
    }

    public async Task<List<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days = 30)
    {
        var candles = await GetOHLCVAsync(symbol, "1d", days);
        return candles.Select(c => new HistoricalPrice
        {
            Timestamp = c.Timestamp,
            Price     = c.Close
        }).ToList();
    }

    public async Task<List<Candle>> GetOHLCVAsync(string symbol, string interval = "1d", int days = 100)
    {
        string mapped = MapSymbol(symbol);
        string range = days switch
        {
            <= 1   => "1d",
            <= 5   => "5d",
            <= 30  => "1mo",
            <= 90  => "3mo",
            <= 180 => "6mo",
            <= 365 => "1y",
            _      => "2y"
        };

        // Yahoo intervals: 1m, 5m, 15m, 30m, 1h, 1d
        string yahooInterval = interval.ToLower() switch
        {
            "1m"  => "1m",
            "5m"  => "5m",
            "15m" => "15m",
            "30m" => "30m",
            "1h"  => "1h",
            "4h"  => "1h", // Yahoo does not support 4h directly on free query, use 1h as fallback
            _     => "1d"
        };

        string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(mapped)}?interval={yahooInterval}&range={range}";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var chart = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
        
        var timestamps = chart.GetProperty("timestamp");
        var quote      = chart.GetProperty("indicators").GetProperty("quote")[0];
        
        var opens   = quote.GetProperty("open");
        var highs   = quote.GetProperty("high");
        var lows    = quote.GetProperty("low");
        var closes  = quote.GetProperty("close");
        var volumes = quote.GetProperty("volume");

        var candles = new List<Candle>();
        int count = timestamps.GetArrayLength();

        for (int i = 0; i < count; i++)
        {
            // Skip nulls or invalid values
            if (opens[i].ValueKind == JsonValueKind.Null ||
                highs[i].ValueKind == JsonValueKind.Null ||
                lows[i].ValueKind == JsonValueKind.Null ||
                closes[i].ValueKind == JsonValueKind.Null)
                continue;

            long tsSecs = timestamps[i].GetInt64();
            decimal vol = volumes[i].ValueKind != JsonValueKind.Null ? volumes[i].GetDecimal() : 0m;

            candles.Add(new Candle
            {
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(tsSecs).UtcDateTime,
                Open      = opens[i].GetDecimal(),
                High      = highs[i].GetDecimal(),
                Low       = lows[i].GetDecimal(),
                Close     = closes[i].GetDecimal(),
                Volume    = vol,
                Symbol    = symbol.ToUpper(),
                Timeframe = interval
            });
        }

        return candles.OrderBy(c => c.Timestamp).ToList();
    }

    public async Task<NewsAnalysis> GetNewsAsync(string asset)
    {
        try
        {
            string encoded = Uri.EscapeDataString(asset);
            string url = $"https://query1.finance.yahoo.com/v1/finance/search?q={encoded}&newsCount=5";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new NewsAnalysis { Sentiment = "Neutral", Score = 50 };

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (!doc.RootElement.TryGetProperty("news", out var newsEl) || newsEl.ValueKind != JsonValueKind.Array)
                return new NewsAnalysis { Sentiment = "Neutral", Score = 50 };

            int scoreTotal = 0;
            int count = 0;

            foreach (var element in newsEl.EnumerateArray())
            {
                string title = element.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(title)) continue;

                var (sentiment, score) = AnalyzeSentimentText(title);
                scoreTotal += (int)(score * 50m + 50m); // map -1..1 to 0..100
                count++;
            }

            int finalScore = count > 0 ? scoreTotal / count : 50;
            string finalSentiment = finalScore > 55 ? "Bullish" : finalScore < 45 ? "Bearish" : "Neutral";

            return new NewsAnalysis
            {
                Sentiment = finalSentiment,
                Score     = finalScore
            };
        }
        catch
        {
            return new NewsAnalysis { Sentiment = "Neutral", Score = 50 };
        }
    }

    private static (string sentiment, decimal score) AnalyzeSentimentText(string text)
    {
        string lower = text.ToLower();
        int score = 0;

        string[] bullWords = { "surge", "rally", "rise", "gain", "buy", "bullish", "growth", "positive", "strong", "beat", "exceed", "record high" };
        string[] bearWords = { "crash", "fall", "drop", "decline", "sell", "bearish", "loss", "negative", "weak", "miss", "disappoint", "concern" };

        foreach (var word in bullWords) if (lower.Contains(word)) score++;
        foreach (var word in bearWords) if (lower.Contains(word)) score--;

        string sentiment = score > 0 ? "Bullish" : score < 0 ? "Bearish" : "Neutral";
        decimal val = score > 0 ? 0.5m : score < 0 ? -0.5m : 0m;
        return (sentiment, val);
    }
}
