using System.Text.Json;
using Atlas.Finance.Models.Fundamental;

namespace Atlas.Finance.Analysis.Fundamental;

/// <summary>
/// Validates financial news using public search endpoints to form a validated sentiment consensus.
/// </summary>
public class NewsValidator
{
    private readonly HttpClient _http;

    public NewsValidator(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AtlasOS/1.0");
    }

    public async Task<NewsValidationReport> ValidateAsync(string asset)
    {
        var report = new NewsValidationReport
        {
            Asset       = asset,
            ValidatedAt = DateTime.UtcNow
        };

        try
        {
            string encoded = Uri.EscapeDataString(asset);
            string url = $"https://query1.finance.yahoo.com/v1/finance/search?q={encoded}&newsCount=15&enableFuzzyQuery=false";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                report.Summary = $"Could not retrieve news from validation source. Status: {response.StatusCode}";
                return report;
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (!doc.RootElement.TryGetProperty("news", out var newsArray) || newsArray.ValueKind != JsonValueKind.Array)
            {
                report.Summary = "No news articles found for the given asset query.";
                return report;
            }

            foreach (var element in newsArray.EnumerateArray())
            {
                string title     = element.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                string publisher = element.TryGetProperty("publisher", out var p) ? p.GetString() ?? "" : "";
                string link      = element.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "";
                long pubTime     = element.TryGetProperty("providerPublishTime", out var pt) ? pt.GetInt64() : 0;

                var item = new NewsItem
                {
                    Title       = title,
                    Source      = publisher,
                    Url         = link,
                    PublishedAt = pubTime > 0 ? DateTimeOffset.FromUnixTimeSeconds(pubTime).UtcDateTime : DateTime.UtcNow
                };

                // Perform keyword-based sentiment analysis
                var (sentiment, score) = AnalyzeSentimentText(title);
                item.Sentiment = sentiment;
                item.SentimentScore = score;
                item.Relevance = title.Contains(asset, StringComparison.OrdinalIgnoreCase) ? 0.9m : 0.5m;

                report.Items.Add(item);
                report.Headlines.Add(title);

                if (sentiment == "Bullish")      report.BullishCount++;
                else if (sentiment == "Bearish") report.BearishCount++;
                else                             report.NeutralCount++;
            }

            // Consensus
            if (report.BullishCount > report.BearishCount)
                report.ConsensusSentiment = "Bullish";
            else if (report.BearishCount > report.BullishCount)
                report.ConsensusSentiment = "Bearish";
            else
                report.ConsensusSentiment = "Neutral";

            int total = report.BullishCount + report.BearishCount + report.NeutralCount;
            if (total > 0)
            {
                int maxSentimentCount = Math.Max(report.BullishCount, report.BearishCount);
                // Confidence formula based on article count and agreement
                report.ConfidenceScore = Math.Min(100, (total * 4) + (maxSentimentCount * 50 / total));
            }
            else
            {
                report.ConsensusSentiment = "Neutral";
                report.ConfidenceScore    = 50;
            }

            // Flags / Inconsistencies
            if (report.BullishCount > 0 && report.BearishCount > 0)
            {
                decimal bullRatio = (decimal)report.BullishCount / total;
                decimal bearRatio = (decimal)report.BearishCount / total;
                if (bullRatio > 0.3m && bearRatio > 0.3m)
                {
                    report.Inconsistencies.Add("Mixed signals detected: High volume of conflicting bullish and bearish articles.");
                }
            }

            if (report.Items.Any() && report.Items.All(i => (DateTime.UtcNow - i.PublishedAt).TotalDays > 7))
            {
                report.Inconsistencies.Add("Stale news: All validated news reports are over 7 days old.");
            }

            report.Summary = $"Validated {total} news sources. Consensus is {report.ConsensusSentiment} " +
                             $"with a confidence score of {report.ConfidenceScore}%. " +
                             $"({report.BullishCount} Bullish, {report.BearishCount} Bearish, {report.NeutralCount} Neutral)";
        }
        catch (Exception ex)
        {
            report.Summary = $"Error validating news feed: {ex.Message}";
        }

        return report;
    }

    private static (string sentiment, decimal score) AnalyzeSentimentText(string text)
    {
        string lower = text.ToLower();
        int score = 0;

        string[] bullWords = { "surge", "rally", "rise", "gain", "buy", "bullish", "growth", "positive", "strong", "beat", "exceed", "record high", "outperform", "upgrade", "soar", "pump" };
        string[] bearWords = { "crash", "fall", "drop", "decline", "sell", "bearish", "loss", "negative", "weak", "miss", "disappoint", "concern", "warning", "risk", "downgrade", "plunge", "dump" };

        foreach (var word in bullWords)  if (lower.Contains(word)) score++;
        foreach (var word in bearWords)  if (lower.Contains(word)) score--;

        string sentiment = score > 0 ? "Bullish" : score < 0 ? "Bearish" : "Neutral";
        decimal normalizedScore = score > 0 ? Math.Min(1.0m, score * 0.3m) : Math.Max(-1.0m, score * 0.3m);

        return (sentiment, normalizedScore);
    }
}
