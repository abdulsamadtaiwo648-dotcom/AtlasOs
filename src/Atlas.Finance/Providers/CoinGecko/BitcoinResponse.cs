using System.Text.Json.Serialization;

namespace Atlas.Finance.Providers.CoinGecko;

public class BitcoinResponse
{
    [JsonPropertyName("usd")]
    public decimal Usd { get; set; }
}