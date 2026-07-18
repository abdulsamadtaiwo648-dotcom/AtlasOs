using System.Text.Json;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;

namespace Atlas.Finance.Providers;

public class AlphaVantageProvider : IFinanceProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AlphaVantageProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY") ?? "demo";
    }

    public Task<CryptoQuote> GetCryptoPriceAsync(string symbol)
    {
        throw new NotImplementedException("Use CoinGeckoProvider for crypto prices.");
    }

    public async Task<StockQuote> GetStockPriceAsync(string symbol)
    {
        string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);
        
        if (!document.RootElement.TryGetProperty("Global Quote", out JsonElement quote))
        {
            throw new Exception("Invalid response from AlphaVantage API.");
        }

        return new StockQuote
        {
            Symbol = symbol.ToUpper(),
            Name = symbol.ToUpper(),
            Price = decimal.TryParse(quote.GetProperty("05. price").GetString(), out var p) ? p : 0,
            ChangePercent = decimal.TryParse(quote.GetProperty("10. change percent").GetString()?.TrimEnd('%'), out var cp) ? cp : 0,
            Volume = decimal.TryParse(quote.GetProperty("06. volume").GetString(), out var v) ? v : 0,
            Open = decimal.TryParse(quote.GetProperty("02. open").GetString(), out var o) ? o : 0,
            High = decimal.TryParse(quote.GetProperty("03. high").GetString(), out var h) ? h : 0,
            Low = decimal.TryParse(quote.GetProperty("04. low").GetString(), out var l) ? l : 0,
            PreviousClose = decimal.TryParse(quote.GetProperty("08. previous close").GetString(), out var pc) ? pc : 0,
            LastUpdated = DateTime.UtcNow
        };
    }

    public Task<EconomicSummary> GetEconomicSummaryAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<NewsAnalysis> GetNewsAsync(string asset)
    {
        string url = $"https://www.alphavantage.co/query?function=NEWS_SENTIMENT&tickers={asset}&apikey={_apiKey}";

        HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);

        var newsAnalysis = new NewsAnalysis();

        if (document.RootElement.TryGetProperty("feed", out JsonElement feed) && feed.ValueKind == JsonValueKind.Array)
        {
            int count = 0;
            decimal totalSentiment = 0;

            foreach (JsonElement item in feed.EnumerateArray())
            {
                if (count >= 10) break; // Analyze top 10 articles

                string title = item.GetProperty("title").GetString() ?? "";
                newsAnalysis.Headlines.Add(title);

                if (item.TryGetProperty("overall_sentiment_score", out JsonElement sentimentScore))
                {
                    if (decimal.TryParse(sentimentScore.GetString(), out decimal score))
                    {
                        totalSentiment += score;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                decimal avgSentiment = totalSentiment / count;
                newsAnalysis.Score = (int)(avgSentiment * 100);

                if (avgSentiment >= 0.15m) newsAnalysis.Sentiment = "Bullish";
                else if (avgSentiment <= -0.15m) newsAnalysis.Sentiment = "Bearish";
                else newsAnalysis.Sentiment = "Neutral";
            }
            else
            {
                newsAnalysis.Sentiment = "Neutral";
                newsAnalysis.Score = 50;
            }
        }
        else
        {
            newsAnalysis.Sentiment = "Neutral";
            newsAnalysis.Score = 50;
        }

        return newsAnalysis;
    }

    public async Task<List<Atlas.Finance.Models.Analysis.HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days = 30)
    {
        // For Forex, AlphaVantage requires FX_DAILY, for stocks TIME_SERIES_DAILY. 
        // We will default to TIME_SERIES_DAILY and assume symbol format.
        bool isForex = symbol.Length == 6 && symbol.StartsWith("EUR") || symbol.StartsWith("GBP");
        string function = isForex ? "FX_DAILY" : "TIME_SERIES_DAILY";
        string symbolParam = isForex ? $"from_symbol={symbol[..3]}&to_symbol={symbol[3..]}" : $"symbol={symbol}";
        
        string url = $"https://www.alphavantage.co/query?function={function}&{symbolParam}&apikey={_apiKey}";
        
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);

        string timeSeriesKey = isForex ? "Time Series FX (Daily)" : "Time Series (Daily)";
        if (!document.RootElement.TryGetProperty(timeSeriesKey, out JsonElement timeSeries))
        {
            // API limits or bad symbol
            return new List<Atlas.Finance.Models.Analysis.HistoricalPrice>();
        }

        var result = new List<Atlas.Finance.Models.Analysis.HistoricalPrice>();
        int count = 0;

        // The properties are ordered descending by date usually, but we will sort later
        foreach (var dayProp in timeSeries.EnumerateObject())
        {
            if (count >= days) break;

            if (DateTime.TryParse(dayProp.Name, out DateTime date))
            {
                if (decimal.TryParse(dayProp.Value.GetProperty("4. close").GetString(), out decimal closePrice))
                {
                    result.Add(new Atlas.Finance.Models.Analysis.HistoricalPrice
                    {
                        Timestamp = date,
                        Price = closePrice
                    });
                    count++;
                }
            }
        }

        return result.OrderBy(r => r.Timestamp).ToList();
    }

    public Task<List<Candle>> GetOHLCVAsync(string symbol, string interval = "1d", int days = 100)
    {
        throw new NotImplementedException("AlphaVantageProvider is deprecated in favor of YahooFinanceProvider.");
    }
}