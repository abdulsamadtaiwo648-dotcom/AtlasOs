using System;
using System.Collections.Generic;
using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Models.Fundamental;

public class NewsValidationReport
{
    public string Asset { get; set; } = "";
    public int BearishCount { get; set; }
    public int BullishCount { get; set; }
    public int ConfidenceScore { get; set; }
    public string ConsensusSentiment { get; set; } = "";
    public List<string> Headlines { get; set; } = new();
    public List<string> Inconsistencies { get; set; } = new();
    public List<NewsItem> Items { get; set; } = new();
    public int NeutralCount { get; set; }
    public string Summary { get; set; } = "";
    public DateTime ValidatedAt { get; set; }
}

public class MacroData
{
    public decimal DXY { get; set; }
    public string DXYTrend { get; set; } = "";
    public string DollarStrength { get; set; } = "";
    public decimal EURInterestRate { get; set; }
    public decimal FearGreedIndex { get; set; }
    public string FearGreedLabel { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public decimal JPYInterestRate { get; set; }
    public string MacroOutlook { get; set; } = "";
    public string NextCPIDate { get; set; } = "";
    public string NextFOMCDate { get; set; } = "";
    public string NextNFPDate { get; set; } = "";
    public string RiskEnvironment { get; set; } = "";
    public decimal UKInterestRate { get; set; }
    public decimal USInterestRate { get; set; }
    public decimal VIX { get; set; }
    public string VIXSignal { get; set; } = "";
    public string YieldCurveSignal { get; set; } = "";
    public decimal TreasuryYield2Y { get; set; }
    public decimal TreasuryYield10Y { get; set; }
}

public class NewsItem
{
    public string Title { get; set; } = "";
    public string Source { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public string Sentiment { get; set; } = "";
    public decimal SentimentScore { get; set; }
    public decimal Relevance { get; set; }
}

