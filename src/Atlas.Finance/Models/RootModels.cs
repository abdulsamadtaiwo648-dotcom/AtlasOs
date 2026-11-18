using System;
using System.Collections.Generic;
using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Models;

public class Candle
{
    public decimal BodySize { get; set; }
    public decimal Close { get; set; }
    public decimal High { get; set; }
    public bool IsBullish { get; set; }
    public decimal Low { get; set; }
    public decimal LowerWick { get; set; }
    public decimal Open { get; set; }
    public decimal Range { get; set; }
    public string Symbol { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public decimal TypicalPrice { get; set; }
    public decimal UpperWick { get; set; }
    public decimal Volume { get; set; }
}

public class MarketSummary
{
    public decimal ChangePercent { get; set; }
    public decimal Confidence { get; set; }
    public decimal MarketCap { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Recommendation { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Trend { get; set; } = "";
    public decimal Volume { get; set; }
}

public class EconomicSummary
{
    public string Country { get; set; } = "";
    public decimal GdpGrowth { get; set; }
    public decimal InflationRate { get; set; }
    public decimal InterestRate { get; set; }
    public string Outlook { get; set; } = "";
    public decimal UnemploymentRate { get; set; }
}

public class NewsAnalysis
{
    public List<string> Headlines { get; set; } = new();
    public decimal Score { get; set; }
    public string Sentiment { get; set; } = "";
}

public class TradingReport
{
    public decimal CurrentPrice { get; set; }
    public string DailyBias { get; set; } = "";
    public decimal MACD { get; set; }
    public decimal RSI { get; set; }
    public string Symbol { get; set; } = "";
    public string Trend { get; set; } = "";
    public decimal SMA50 { get; set; }
    public decimal SMA200 { get; set; }
    public decimal EMA20 { get; set; }
}

public class StockQuote
{
    public decimal ChangePercent { get; set; }
    public decimal High { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal Low { get; set; }
    public decimal MarketCap { get; set; }
    public string Name { get; set; } = "";
    public decimal Open { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal Price { get; set; }
    public string Symbol { get; set; } = "";
    public decimal Volume { get; set; }
}

public class CryptoQuote
{
    public string Currency { get; set; } = "";
    public DateTime LastUpdated { get; set; }
    public decimal MarketCap { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Symbol { get; set; } = "";
    public decimal Volume { get; set; }
    public decimal Change24Hours { get; set; }
}

public class RiskAnalysis
{
    public string Level { get; set; } = "";
    public List<string> Reasons { get; set; } = new();
    public string RiskLevel { get; set; } = "";
    public decimal RiskRewardRatio { get; set; }
    public decimal RiskScore { get; set; }
    public decimal Score { get; set; }
    public decimal StopLossPercent { get; set; }
    public decimal SuggestedPositionSize { get; set; }
    public string Symbol { get; set; } = "";
    public decimal TakeProfitPercent { get; set; }
    public decimal Volatility { get; set; }
}

public class PortfolioAnalysis
{
    public decimal AverageChange { get; set; }
    public string BestPerformer { get; set; } = "";
    public decimal DiversificationScore { get; set; }
    public string HighestValueAsset { get; set; } = "";
    public string OverallTrend { get; set; } = "";
    public string PortfolioRisk { get; set; } = "";
    public List<string> Recommendations { get; set; } = new();
    public int TotalAssets { get; set; }
    public decimal TotalMarketValue { get; set; }
    public string WorstPerformer { get; set; } = "";
}

public class FullAnalysisReport
{
    public string ActiveSession { get; set; } = "";
    public string AssetType { get; set; } = "";
    public decimal BearishProbability { get; set; }
    public string BearishScenario { get; set; } = "";
    public decimal BullishProbability { get; set; }
    public string BullishScenario { get; set; } = "";
    public List<CandlePattern> CandlePatterns { get; set; } = new();
    public List<ChartPattern> ChartPatterns { get; set; } = new();
    public string Currency { get; set; } = "";
    public decimal CurrentPrice { get; set; }
    public string CurrentTime { get; set; } = "";
    public string CurrentTrend { get; set; } = "";
    public decimal EntryZoneHigh { get; set; }
    public decimal EntryZoneLow { get; set; }
    public string ExecutiveSummary { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public decimal HighPrice { get; set; }
    public IndicatorSet Indicators { get; set; }
    public string InstitutionalSentiment { get; set; } = "";
    public decimal InvalidationLevel { get; set; }
    public string LongTermOutlook { get; set; } = "";
    public decimal LowPrice { get; set; }
    public MacroData Macro { get; set; }
    public string MacroAnalysis { get; set; } = "";
    public decimal MarketCap { get; set; }
    public string MediumTermOutlook { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal NeutralProbability { get; set; }
    public NewsValidationReport News { get; set; }
    public string NewsImpact { get; set; } = "";
    public string NewsSentiment { get; set; } = "";
    public decimal OpenPrice { get; set; }
    public int OverallConfidence { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal PriceChange { get; set; }
    public decimal PriceChangePercent { get; set; }
    public int ProbabilityScore { get; set; }
    public List<string> Reasoning { get; set; } = new();
    public string Recommendation { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public decimal RiskRewardRatio { get; set; }
    public decimal RiskScore { get; set; }
    public string ShortTermOutlook { get; set; } = "";
    public SmartMoneyAnalysis SmartMoney { get; set; }
    public decimal StopLoss { get; set; }
    public decimal StopLossPercent { get; set; }
    public decimal SuggestedPositionSize { get; set; }
    public string Symbol { get; set; } = "";
    public decimal TakeProfit { get; set; }
    public decimal TakeProfitPercent { get; set; }
    public decimal Volume { get; set; }
    public List<string> Warnings { get; set; } = new();
    public decimal TakeProfit1 { get; set; }
    public decimal TakeProfit2 { get; set; }
    public decimal TakeProfit3 { get; set; }
}

public class ForecastResult
{
    public decimal BearishProbability { get; set; }
    public decimal BullishProbability { get; set; }
    public decimal NeutralProbability { get; set; }
    public string Recommendation { get; set; } = "";
}

public class InvestmentAnalysis
{
    public decimal ExpectedReturn { get; set; }
    public string Rating { get; set; } = "";
    public string Recommendation { get; set; } = "";
}

public class CurrencyQuote
{
}

public class TechnicalAnalysis
{
    public decimal EntryPrice { get; set; }
    public List<string> Indicators { get; set; } = new();
    public string Momentum { get; set; } = "";
    public List<string> Reasons { get; set; } = new();
    public decimal Resistance { get; set; }
    public string Signal { get; set; } = "";
    public decimal StopLoss { get; set; }
    public decimal Strength { get; set; }
    public decimal Support { get; set; }
    public string Symbol { get; set; } = "";
    public decimal TakeProfit { get; set; }
    public string Trend { get; set; } = "";
}

