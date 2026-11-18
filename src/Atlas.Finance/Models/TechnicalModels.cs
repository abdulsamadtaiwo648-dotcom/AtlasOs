using System;
using System.Collections.Generic;
using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Models.Technical;

public class IndicatorSet
{
    public decimal ADX { get; set; }
    public string ADXSignal { get; set; } = "";
    public decimal ATR { get; set; }
    public decimal ATRPercent { get; set; }
    public decimal AvgVolume { get; set; }
    public int BearishIndicators { get; set; }
    public decimal BollingerLower { get; set; }
    public decimal BollingerMiddle { get; set; }
    public string BollingerSignal { get; set; } = "";
    public decimal BollingerUpper { get; set; }
    public decimal BollingerWidth { get; set; }
    public int BullishIndicators { get; set; }
    public decimal CMF { get; set; }
    public string CMFSignal { get; set; } = "";
    public decimal CurrentPrice { get; set; }
    public bool DeathCross { get; set; }
    public Dictionary<string, decimal> FibLevels { get; set; } = new();
    public bool GoldenCross { get; set; }
    public decimal MACD { get; set; }
    public string MACDCross { get; set; } = "";
    public decimal MACDHistogram { get; set; }
    public decimal MACDSignal { get; set; }
    public decimal MinusDI { get; set; }
    public decimal Momentum { get; set; }
    public string MomentumSignal { get; set; } = "";
    public int NeutralIndicators { get; set; }
    public decimal OBV { get; set; }
    public string OBVTrend { get; set; } = "";
    public string OTEZone { get; set; } = "";
    public string OverallSignal { get; set; } = "";
    public decimal PivotPoint { get; set; }
    public decimal PlusDI { get; set; }
    public string PriceVsVWAP { get; set; } = "";
    public decimal RSI { get; set; }
    public string RSISignal { get; set; } = "";
    public decimal StochRSI { get; set; }
    public string StochRSISignal { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public string TrendStrength { get; set; } = "";
    public decimal VWAP { get; set; }
    public decimal Volatility { get; set; }
    public string VolatilitySignal { get; set; } = "";
    public decimal Volume { get; set; }
    public string VolumeSignal { get; set; } = "";
    public decimal EMA100 { get; set; }
    public decimal SMA20 { get; set; }
    public decimal R3 { get; set; }
    public decimal S3 { get; set; }
    public decimal EMA9 { get; set; }
    public decimal EMA20 { get; set; }
    public decimal EMA50 { get; set; }
    public decimal EMA200 { get; set; }
    public decimal SMA50 { get; set; }
    public decimal SMA200 { get; set; }
    public string PriceVsEMA50 { get; set; } = "";
    public string PriceVsEMA200 { get; set; } = "";
    public decimal R1 { get; set; }
    public decimal R2 { get; set; }
    public decimal S1 { get; set; }
    public decimal S2 { get; set; }
}

public class CandlePattern
{
    public string Description { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Strength { get; set; }
    public DateTime Timestamp { get; set; }
    public string TradingImplication { get; set; } = "";
    public string Type { get; set; } = "";
}

public class ChartPattern
{
    public decimal BreakoutLevel { get; set; }
    public decimal Confidence { get; set; }
    public string Description { get; set; } = "";
    public DateTime EndTimestamp { get; set; }
    public bool IsConfirmed { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartTimestamp { get; set; }
    public decimal StopLoss { get; set; }
    public decimal TargetPrice { get; set; }
    public string Type { get; set; } = "";
}

