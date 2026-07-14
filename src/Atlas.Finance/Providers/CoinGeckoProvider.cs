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
}