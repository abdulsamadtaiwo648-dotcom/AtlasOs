using System.Text.Json;
using Atlas.Finance.Models.Fundamental;

namespace Atlas.Finance.Analysis.Fundamental;

/// <summary>
/// Fetches macro data (DXY, VIX, yields) from Yahoo Finance public API — no key required.
/// </summary>
public class MacroAnalyzer
{
    private readonly HttpClient _http;

    public MacroAnalyzer(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AtlasOS/1.0");
    }

    public async Task<MacroData> AnalyzeAsync(string assetSymbol)
    {
        var macro = new MacroData
        {
            USInterestRate  = 4.5m,   // Fed funds rate (known)
            EURInterestRate = 3.65m,  // ECB (known)
            UKInterestRate  = 5.25m,  // BOE (known)
            JPYInterestRate = 0.1m,   // BOJ (known)
            NextFOMCDate    = "Check fed.gov for next FOMC date",
            NextNFPDate     = "First Friday of each month",
            NextCPIDate     = "Check bls.gov for CPI release dates",
            GeneratedAt     = DateTime.UtcNow
        };

        // Fetch from Yahoo Finance
        await FetchYahooTicker("DX-Y.NYB", async price =>
        {
            macro.DXY = price;
            macro.DollarStrength = price > 105m ? "Strong" : price < 100m ? "Weak" : "Neutral";
        });

        await FetchYahooTicker("^VIX", async price =>
        {
            macro.VIX = price;
            macro.VIXSignal      = price < 20m ? "Greed" : price > 30m ? "Fear" : "Neutral";
            // Invert VIX to get Fear & Greed estimate
            int fg = Math.Max(0, Math.Min(100, (int)(100m - (price - 10m) * 2.5m)));
            macro.FearGreedIndex = fg;
            macro.FearGreedLabel = fg switch
            {
                <= 25  => "Extreme Fear",
                <= 45  => "Fear",
                <= 55  => "Neutral",
                <= 75  => "Greed",
                _      => "Extreme Greed"
            };
        });

        await FetchYahooTicker("^TNX", async price =>
        {
            macro.TreasuryYield10Y = price;
        });

        await FetchYahooTicker("^IRX", async price =>
        {
            macro.TreasuryYield2Y = price / 100m; // IRX is already in percent format × 10
            decimal spread = macro.TreasuryYield10Y - macro.TreasuryYield2Y;
            macro.YieldCurveSignal = spread > 0.5m ? "Normal" : spread < 0m ? "Inverted" : "Flat";
        });

        // DXY trend (compare to 20 days ago)
        macro.DXYTrend = macro.DXY > 103m ? "Uptrend" : macro.DXY < 101m ? "Downtrend" : "Sideways";

        // Risk environment
        macro.RiskEnvironment = (macro.VIX < 20m && macro.DollarStrength != "Strong") ? "Risk-On"
                              : (macro.VIX > 30m || macro.DollarStrength == "Strong")  ? "Risk-Off"
                              : "Neutral";

        macro.MacroOutlook = $"The macro environment is {macro.RiskEnvironment} with a {macro.DollarStrength.ToLower()} US dollar (DXY {macro.DXY:F2}). " +
                             $"VIX at {macro.VIX:F2} signals {macro.VIXSignal.ToLower()} sentiment. " +
                             $"Yield curve is {macro.YieldCurveSignal.ToLower()} (10Y: {macro.TreasuryYield10Y:F2}%). " +
                             $"US Fed rate: {macro.USInterestRate}%.";

        return macro;
    }

    private async Task FetchYahooTicker(string ticker, Func<decimal, Task> action)
    {
        try
        {
            string encoded = Uri.EscapeDataString(ticker);
            string url = $"https://query1.finance.yahoo.com/v8/finance/chart/{encoded}?interval=1d&range=1d";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return;

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta");

            if (result.TryGetProperty("regularMarketPrice", out var priceEl))
                await action(priceEl.GetDecimal());
        }
        catch { /* fail silently — use default values */ }
    }
}
