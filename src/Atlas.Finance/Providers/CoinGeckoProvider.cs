using System.Text.Json;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;


namespace Atlas.Finance.Providers;

public class CoinGeckoProvider : IFinanceProvider
{
    private readonly HttpClient _httpClient;

    public CoinGeckoProvider(HttpClient httpClient)
{
    _httpClient = httpClient;

    _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
        "AtlasOS/0.1 (https://github.com/abdulsamadtaiwo648-dotcom/AtlasOS)");

    _httpClient.DefaultRequestHeaders.Accept.ParseAdd(
        "application/json");
}

    public async Task<CryptoQuote> GetCryptoPriceAsync(string symbol)
    {
        string coin = symbol.ToLower() switch
        {
            "btc" => "bitcoin",
            "eth" => "ethereum",
            "sol" => "solana",
            "bnb" => "binancecoin",
            "doge" => "dogecoin",
            "xrp" => "ripple",
            "ada" => "cardano",
            _ => symbol.ToLower()
        };

        string url =
            $"https://api.coingecko.com/api/v3/simple/price" +
            $"?ids={coin}" +
            $"&vs_currencies=usd" +
            $"&include_market_cap=true" +
            $"&include_24hr_vol=true" +
            $"&include_24hr_change=true";

        HttpResponseMessage httpResponse =
            await _httpClient.GetAsync(url);

        string body = await httpResponse.Content.ReadAsStringAsync();

if (!httpResponse.IsSuccessStatusCode)
{
    throw new Exception(
        $"CoinGecko returned {(int)httpResponse.StatusCode}\n\n{body}");
}

        string json =
            await httpResponse.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement asset =
            document.RootElement.GetProperty(coin);

        return new CryptoQuote
        {
            Symbol = symbol.ToUpper(),

            Name = coin,

            Price = asset.GetProperty("usd").GetDecimal(),

            Change24Hours =
                asset.GetProperty("usd_24h_change").GetDecimal(),

            Volume =
                asset.GetProperty("usd_24h_vol").GetDecimal(),

            MarketCap =
                asset.GetProperty("usd_market_cap").GetDecimal(),

            Currency = "USD",

            LastUpdated = DateTime.UtcNow
        };
    }
    public Task<StockQuote> GetStockPriceAsync(string symbol)
    {
        throw new NotImplementedException();
    }
    public Task<EconomicSummary> GetEconomicSummaryAsync()
    {
        throw new NotImplementedException();
    }

    public Task<CurrencyQuote> ConvertCurrencyAsync(
        decimal amount,
        string from,
        string to)
    {
        throw new NotImplementedException();
    }

    public async Task<List<Atlas.Finance.Models.Analysis.HistoricalPrice>> GetHistoricalPricesAsync(string symbol, int days = 30)
    {
        string coin = symbol.ToLower() switch
        {
            "btc" => "bitcoin",
            "eth" => "ethereum",
            "sol" => "solana",
            "bnb" => "binancecoin",
            "doge" => "dogecoin",
            "xrp" => "ripple",
            "ada" => "cardano",
            _ => symbol.ToLower()
        };

        string url = $"https://api.coingecko.com/api/v3/coins/{coin}/market_chart?vs_currency=usd&days={days}";

        HttpResponseMessage httpResponse = await _httpClient.GetAsync(url);
        
        if (!httpResponse.IsSuccessStatusCode)
            throw new Exception($"CoinGecko returned {(int)httpResponse.StatusCode}");

        string json = await httpResponse.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);

        var prices = document.RootElement.GetProperty("prices");
        var result = new List<Atlas.Finance.Models.Analysis.HistoricalPrice>();

        foreach (var p in prices.EnumerateArray())
        {
            long timestampMs = p[0].GetInt64();
            decimal price = p[1].GetDecimal();

            result.Add(new Atlas.Finance.Models.Analysis.HistoricalPrice
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime,
                Price = price
            });
        }

        return result;
    }

    public async Task<List<Candle>> GetOHLCVAsync(string symbol, string interval = "1d", int days = 100)
    {
        string coin = symbol.ToLower() switch
        {
            "btc" => "bitcoin",
            "btc-usd" => "bitcoin",
            "eth" => "ethereum",
            "eth-usd" => "ethereum",
            "sol" => "solana",
            "sol-usd" => "solana",
            "bnb" => "binancecoin",
            "bnb-usd" => "binancecoin",
            "doge" => "dogecoin",
            "doge-usd" => "dogecoin",
            "xrp" => "ripple",
            "xrp-usd" => "ripple",
            "ada" => "cardano",
            "ada-usd" => "cardano",
            _ => symbol.ToLower().Replace("-usd", "")
        };

        int cgDays = days switch
        {
            <= 1 => 1,
            <= 7 => 7,
            <= 14 => 14,
            <= 30 => 30,
            <= 90 => 90,
            <= 180 => 180,
            _ => 365
        };

        string url = $"https://api.coingecko.com/api/v3/coins/{coin}/ohlc?vs_currency=usd&days={cgDays}";
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"CoinGecko OHLC returned {(int)response.StatusCode}");
        }

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        var candles = new List<Candle>();

        foreach (var arr in doc.RootElement.EnumerateArray())
        {
            long ms = arr[0].GetInt64();
            candles.Add(new Candle
            {
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime,
                Open      = arr[1].GetDecimal(),
                High      = arr[2].GetDecimal(),
                Low       = arr[3].GetDecimal(),
                Close     = arr[4].GetDecimal(),
                Volume    = 1000m, // CoinGecko OHLC API does not return volume, mock it
                Symbol    = symbol.ToUpper(),
                Timeframe = interval
            });
        }

        return candles;
    }

    public Task<NewsAnalysis> GetNewsAsync(string asset)
    {
        return Task.FromResult(new NewsAnalysis
        {
            Sentiment = "Neutral",
            Score     = 50
        });
    }
}