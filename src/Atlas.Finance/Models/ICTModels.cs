using System;
using System.Collections.Generic;
using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Models.ICT;

public class OrderBlock
{
    public decimal Close { get; set; }
    public string Description { get; set; } = "";
    public decimal High { get; set; }
    public bool IsMitigated { get; set; }
    public bool IsValid { get; set; }
    public decimal Low { get; set; }
    public decimal Open { get; set; }
    public decimal Strength { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Zone { get; set; } = "";
}

public class FairValueGap
{
    public string Description { get; set; } = "";
    public decimal High { get; set; }
    public bool IsFilled { get; set; }
    public bool IsPartiallyFilled { get; set; }
    public decimal Low { get; set; }
    public decimal Midpoint { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
}

public class LiquidityLevel
{
    public string Description { get; set; } = "";
    public decimal High { get; set; }
    public bool IsSwept { get; set; }
    public decimal Low { get; set; }
    public decimal Price { get; set; }
    public decimal Strength { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = "";
}

public class MarketStructure
{
    public string CurrentTrend { get; set; } = "";
    public decimal DiscountZone { get; set; }
    public decimal EquilibriumZone { get; set; }
    public List<StructureEvent> Events { get; set; } = new();
    public string ExternalTrend { get; set; } = "";
    public string InternalTrend { get; set; } = "";
    public bool IsInDiscount { get; set; }
    public bool IsInPremium { get; set; }
    public decimal LastSwingHigh { get; set; }
    public DateTime LastSwingHighTime { get; set; }
    public decimal LastSwingLow { get; set; }
    public DateTime LastSwingLowTime { get; set; }
    public decimal PremiumZone { get; set; }
    public string Symbol { get; set; } = "";
}

public class SmartMoneyAnalysis
{
    public string ActiveKillZone { get; set; } = "";
    public string DailyBias { get; set; } = "";
    public bool EngineeredLiquidity { get; set; }
    public bool Expansion { get; set; }
    public List<FairValueGap> FairValueGaps { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public bool InstitutionalAccumulation { get; set; }
    public bool InstitutionalDistribution { get; set; }
    public bool IsInOTE { get; set; }
    public bool IsJudasSwing { get; set; }
    public string JudasDirection { get; set; } = "";
    public List<LiquidityLevel> LiquidityLevels { get; set; } = new();
    public MarketStructure MarketStructure { get; set; }
    public string MonthlyBias { get; set; } = "";
    public decimal OTEZoneHigh { get; set; }
    public decimal OTEZoneLow { get; set; }
    public List<OrderBlock> OrderBlocks { get; set; } = new();
    public List<string> Reasons { get; set; } = new();
    public bool Rebalancing { get; set; }
    public bool RetailTrapDetected { get; set; }
    public bool Retracement { get; set; }
    public string SMTDescription { get; set; } = "";
    public bool SMTDivergence { get; set; }
    public bool StopHuntDetected { get; set; }
    public string StopHuntDirection { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public string WeeklyBias { get; set; } = "";
}

public class StructureEvent
{
    public string Type { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = "";
}

