using System.Text;
using Atlas.Finance.Analysis;
using Atlas.Finance.Analysis.ICT;
using Atlas.Finance.Analysis.Technical;
using Atlas.Finance.Analysis.Fundamental;
using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;
using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;
using Atlas.Finance.Providers;

namespace Atlas.Finance.Engines;

public class FinanceEngine : IFinanceEngine
{
    private readonly IMarketEngine _market;
    private readonly IEconomyEngine _economy;
    private readonly RiskAnalyzer _risk;
    private readonly PortfolioAnalyzer _portfolio;
    private readonly TechnicalAnalyzer _technical;
    private readonly InvestmentAnalyzer _investment;
    private readonly ChartEngine _chart;
    private readonly IFinanceProvider _provider;

    private readonly NewsValidator _newsValidator;
    private readonly MacroAnalyzer _macro;
    private readonly ForecastEngine _forecast;

    public FinanceEngine(
        IMarketEngine market,
        IEconomyEngine economy,
        RiskAnalyzer risk,
        PortfolioAnalyzer portfolio,
        TechnicalAnalyzer technical,
        InvestmentAnalyzer investment,
        ChartEngine chart,
        IFinanceProvider provider,
        HttpClient httpClient)
    {
        _market = market;
        _economy = economy;
        _risk = risk;
        _portfolio = portfolio;
        _technical = technical;
        _investment = investment;
        _chart = chart;
        _provider = provider;

        _newsValidator = new NewsValidator(httpClient);
        _macro = new MacroAnalyzer(httpClient);
        _forecast = new ForecastEngine();
    }

    // Used by Atlas AI to get clean context
    public async Task<string> BuildContextAsync(string input)
    {
        var (symbol, assetType) = DetectAsset(input);
        if (string.IsNullOrEmpty(symbol))
        {
            return $"[Finance System Time: {DateTime.Now:dddd, dd MMMM yyyy hh:mm:ss tt}] No financial asset detected in input. Mention an asset like Bitcoin, Apple, or EUR/USD to analyze.";
        }

        try
        {
            var report = await AnalyzeAsync(symbol, assetType);
            var sb = new StringBuilder();
            sb.AppendLine($"--- FINANCE ENGINE CONTEXT FOR {report.Symbol} ---");
            sb.AppendLine($"Time: {report.CurrentTime} | Active Session: {report.ActiveSession}");
            sb.AppendLine($"Price: ${report.CurrentPrice:N4} ({report.PriceChangePercent:+0.00;-0.00}%)");
            sb.AppendLine($"Trend: {report.CurrentTrend} | Bias: {report.SmartMoney?.DailyBias ?? "Neutral"}");
            sb.AppendLine($"Indicators: RSI={report.Indicators?.RSI:F1} ({report.Indicators?.RSISignal}), MACD={report.Indicators?.MACDHistogram:F4}");
            sb.AppendLine($"Institutional Sentiment: {report.InstitutionalSentiment}");
            if (report.SmartMoney?.OrderBlocks?.Any(ob => ob.IsValid) == true)
            {
                var ob = report.SmartMoney.OrderBlocks.First(ob => ob.IsValid);
                sb.AppendLine($"Order Block: {ob.Zone} OB @ ${ob.Low:N4}-${ob.High:N4}");
            }
            if (report.SmartMoney?.FairValueGaps?.Any(f => !f.IsFilled) == true)
            {
                var fvg = report.SmartMoney.FairValueGaps.First(f => !f.IsFilled);
                sb.AppendLine($"Fair Value Gap: {fvg.Type} @ ${fvg.Low:N4}-${fvg.High:N4}");
            }
            sb.AppendLine($"News Consensus: {report.News?.ConsensusSentiment ?? "Neutral"} (Confidence: {report.News?.ConfidenceScore ?? 0}%)");
            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"[Finance Engine Error analyzing {symbol}: {ex.Message}]";
        }
    }

    // Direct user interaction
    public async Task<string> ProcessAsync(string input)
    {
        var (symbol, assetType) = DetectAsset(input);
        if (string.IsNullOrEmpty(symbol))
        {
            return $"It is currently {DateTime.Now:dddd, dd MMMM yyyy  hh:mm:ss tt}.\nTo analyze markets, please mention an asset ticker or name (e.g. BTC, EUR/USD, Apple, Gold).";
        }

        try
        {
            var report = await AnalyzeAsync(symbol, assetType);
            return FormatReport(report);
        }
        catch (Exception ex)
        {
            return $"Error performing institutional analysis for {symbol}: {ex.Message}\n\nPlease check your internet connection and symbol naming.";
        }
    }

    public async Task<FullAnalysisReport> AnalyzeAsync(string symbol, string assetType)
    {
        // 1. Fetch OHLCV candles
        var dailyCandles = await _provider.GetOHLCVAsync(symbol, "1d", 150);
        if (dailyCandles == null || !dailyCandles.Any())
        {
            throw new Exception($"Failed to retrieve daily OHLCV candles for symbol {symbol}");
        }

        var lastCandle = dailyCandles.Last();
        decimal currentPrice = lastCandle.Close;

        // 2. Perform Technical Indicators calculation
        var indicators = IndicatorEngine.BuildIndicatorSet(dailyCandles, "1D");

        // 3. Recognize patterns
        var candlePatterns = CandlePatternRecognizer.Recognize(dailyCandles);
        var chartPatterns = ChartPatternRecognizer.Recognize(dailyCandles);

        // 4. Run ICT/SMC analysis
        var smc = SmartMoneyAnalyzer.Analyze(dailyCandles, symbol, "1D");

        // 5. News consensus validation
        var newsReport = await _newsValidator.ValidateAsync(symbol);

        // 6. Macro analysis overlay
        var macroData = await _macro.AnalyzeAsync(symbol);

        // 7. Core legacy analyzers for risk
        decimal changePercent = dailyCandles.Count > 1 ? (currentPrice - dailyCandles[^2].Close) / dailyCandles[^2].Close * 100m : 0m;
        var summary = new MarketSummary
        {
            Symbol = symbol,
            Name = symbol,
            Price = currentPrice,
            ChangePercent = changePercent,
            Volume = lastCandle.Volume,
            Trend = smc.MarketStructure?.CurrentTrend ?? "Neutral"
        };
        var risk = _risk.Analyze(summary);

        // 8. Forecast prediction
        var tradingReport = new TradingReport
        {
            Symbol = symbol,
            CurrentPrice = currentPrice,
            RSI = indicators.RSI,
            MACD = indicators.MACD,
            SMA50 = indicators.SMA50,
            SMA200 = indicators.SMA200,
            EMA20 = indicators.EMA20,
            Trend = smc.MarketStructure?.CurrentTrend ?? "Neutral",
            DailyBias = smc.DailyBias
        };
        var forecast = _forecast.Predict(tradingReport);

        // 9. Institutional sentiment + confidence
        string institutionalSentiment = SentimentAnalyzer.AnalyzeInstitutionalSentiment(smc, indicators, newsReport);
        int overallConfidence = SentimentAnalyzer.CalculateOverallConfidence(smc, indicators, newsReport);

        // 10. Scenario targets based on ATR and swing ranges
        decimal atr = indicators.ATR;
        if (atr == 0) atr = currentPrice * 0.02m; // fallback 2% ATR

        decimal stopLoss = smc.DailyBias == "Bullish" 
            ? currentPrice - (atr * 1.5m) 
            : currentPrice + (atr * 1.5m);

        decimal takeProfit1 = smc.DailyBias == "Bullish"
            ? currentPrice + (atr * 2m)
            : currentPrice - (atr * 2m);

        decimal takeProfit2 = smc.DailyBias == "Bullish"
            ? currentPrice + (atr * 3.5m)
            : currentPrice - (atr * 3.5m);

        decimal takeProfit3 = smc.DailyBias == "Bullish"
            ? currentPrice + (atr * 5m)
            : currentPrice - (atr * 5m);

        // 11. Build output scenarios description
        string bullishScenario = $"Price breaks above resistance at ${indicators.R2:N4} confirming structural shift. Targets ${takeProfit2:N4} then ${takeProfit3:N4}. Invalidation below ${stopLoss:N4}.";
        string bearishScenario = $"Rejection at key supply zone near ${indicators.R1:N4} targets equal lows/SSL near ${indicators.S2:N4} and down to ${takeProfit3:N4}. Invalidation above ${stopLoss:N4}.";

        var report = new FullAnalysisReport
        {
            Symbol = symbol,
            Name = newsReport.Asset,
            AssetType = assetType,
            Currency = "USD",
            CurrentPrice = currentPrice,
            OpenPrice = lastCandle.Open,
            HighPrice = lastCandle.High,
            LowPrice = lastCandle.Low,
            PreviousClose = dailyCandles.Count > 1 ? dailyCandles[^2].Close : lastCandle.Open,
            PriceChange = dailyCandles.Count > 1 ? currentPrice - dailyCandles[^2].Close : 0m,
            PriceChangePercent = dailyCandles.Count > 1 ? (currentPrice - dailyCandles[^2].Close) / dailyCandles[^2].Close * 100m : 0m,
            Volume = lastCandle.Volume,
            MarketCap = 0m, // Let provider mock or leave to summary
            GeneratedAt = DateTime.UtcNow,
            CurrentTime = DateTime.Now.ToString("dddd, dd MMMM yyyy  hh:mm:ss tt"),
            ActiveSession = KillZoneDetector.GetCurrentSession(DateTime.UtcNow),
            ExecutiveSummary = BuildExecutiveSummary(symbol, assetType, currentPrice, smc, indicators),
            CurrentTrend = smc.MarketStructure?.CurrentTrend ?? "Neutral",
            SmartMoney = smc,
            Indicators = indicators,
            CandlePatterns = candlePatterns,
            ChartPatterns = chartPatterns,
            News = newsReport,
            Macro = macroData,
            RiskLevel = risk.Level,
            RiskScore = risk.Score,
            StopLoss = stopLoss,
            StopLossPercent = risk.StopLossPercent,
            TakeProfit = takeProfit1,
            TakeProfitPercent = risk.TakeProfitPercent,
            RiskRewardRatio = 2.0m,
            SuggestedPositionSize = risk.SuggestedPositionSize,
            BullishProbability = forecast.BullishProbability,
            BearishProbability = forecast.BearishProbability,
            NeutralProbability = forecast.NeutralProbability,
            ProbabilityScore = overallConfidence,
            BullishScenario = bullishScenario,
            BearishScenario = bearishScenario,
            InvalidationLevel = stopLoss,
            EntryZoneLow = smc.OTEZoneLow > 0 ? smc.OTEZoneLow : currentPrice * 0.99m,
            EntryZoneHigh = smc.OTEZoneHigh > 0 ? smc.OTEZoneHigh : currentPrice * 1.01m,
            TakeProfit1 = takeProfit1,
            TakeProfit2 = takeProfit2,
            TakeProfit3 = takeProfit3,
            ShortTermOutlook = $"Short-term (1-5 days): {smc.DailyBias} bias in play. Watch active {smc.ActiveKillZone} kill zone.",
            MediumTermOutlook = $"Medium-term (1-4 weeks): {smc.MarketStructure?.CurrentTrend ?? "Ranging"} structure confirmed. EMA50/200 trend is {(indicators.GoldenCross ? "Bullish" : "Bearish")}.",
            LongTermOutlook = $"Long-term (1-3 months): Influenced by {macroData.RiskEnvironment} macro overlay and {macroData.DollarStrength.ToLower()} DXY index.",
            InstitutionalSentiment = institutionalSentiment,
            NewsSentiment = newsReport.ConsensusSentiment,
            NewsImpact = newsReport.Summary,
            MacroAnalysis = macroData.MacroOutlook,
            OverallConfidence = overallConfidence,
            Recommendation = forecast.Recommendation,
            Reasoning = smc.Reasons.Concat(indicators.OverallSignal == "Neutral" ? new List<string>() : new[] { $"Indicator consensus reads: {indicators.OverallSignal}" }).ToList(),
            Warnings = smc.Reasons.Where(r => r.Contains("Stop hunt") || r.Contains("trap")).ToList()
        };

        return report;
    }

    private static (string symbol, string assetType) DetectAsset(string input)
    {
        string text = input.ToLower();
        
        // Crypto
        if (text.Contains("bitcoin") || text.Contains(" btc") || text.EndsWith("btc")) return ("BTC-USD", "Crypto");
        if (text.Contains("ethereum") || text.Contains(" eth") || text.EndsWith("eth")) return ("ETH-USD", "Crypto");
        if (text.Contains("solana") || text.Contains(" sol") || text.EndsWith("sol")) return ("SOL-USD", "Crypto");
        if (text.Contains("bnb") || text.Contains("binance coin")) return ("BNB-USD", "Crypto");
        if (text.Contains("doge") || text.Contains("dogecoin")) return ("DOGE-USD", "Crypto");
        if (text.Contains("ripple") || text.Contains(" xrp")) return ("XRP-USD", "Crypto");
        if (text.Contains("ada") || text.Contains("cardano")) return ("ADA-USD", "Crypto");

        // Forex
        if (text.Contains("eurusd") || text.Contains("eur/usd") || text.Contains("euro")) return ("EURUSD=X", "Forex");
        if (text.Contains("gbpusd") || text.Contains("gbp/usd") || text.Contains("pound")) return ("GBPUSD=X", "Forex");
        if (text.Contains("usdjpy") || text.Contains("usd/jpy") || text.Contains("yen")) return ("USDJPY=X", "Forex");
        if (text.Contains("audusd") || text.Contains("aud/usd")) return ("AUDUSD=X", "Forex");
        if (text.Contains("dxy") || text.Contains("dollar index")) return ("DX-Y.NYB", "Index");

        // Stocks
        if (text.Contains("apple") || text.Contains(" aapl")) return ("AAPL", "Stock");
        if (text.Contains("tesla") || text.Contains(" tsla")) return ("TSLA", "Stock");
        if (text.Contains("nvidia") || text.Contains(" nvda")) return ("NVDA", "Stock");
        if (text.Contains("microsoft") || text.Contains(" msft")) return ("MSFT", "Stock");
        if (text.Contains("amazon") || text.Contains(" amzn")) return ("AMZN", "Stock");
        if (text.Contains("google") || text.Contains(" googl") || text.Contains("alphabet")) return ("GOOGL", "Stock");
        if (text.Contains("meta") || text.Contains("facebook")) return ("META", "Stock");

        // Commodities
        if (text.Contains("gold") || text.Contains(" xau")) return ("GC=F", "Commodity");
        if (text.Contains("silver") || text.Contains(" xag")) return ("SI=F", "Commodity");
        if (text.Contains("oil") || text.Contains("crude") || text.Contains("wti")) return ("CL=F", "Commodity");
        if (text.Contains("brent")) return ("BZ=F", "Commodity");

        // Indices
        if (text.Contains("s&p") || text.Contains("sp500") || text.Contains("s&p500")) return ("^GSPC", "Index");
        if (text.Contains("nasdaq")) return ("^IXIC", "Index");
        if (text.Contains("dow jones") || text.Contains("dow")) return ("^DJI", "Index");
        if (text.Contains("vix")) return ("^VIX", "Index");

        return ("", "Unknown");
    }

    private static string BuildExecutiveSummary(string symbol, string assetType, decimal price, SmartMoneyAnalysis smc, IndicatorSet ind)
    {
        return $"{symbol} ({assetType}) is trading at ${price:N4}. Market structure exhibits a {smc.MarketStructure?.CurrentTrend.ToLower()} bias with a daily directional bias of {smc.DailyBias}. " +
               $"Indicators confirm overall signal is {ind.OverallSignal} with {ind.BullishIndicators} bullish versus {ind.BearishIndicators} bearish markers. " +
               $"Order flow highlights {smc.Summary.ToLower()}.";
    }

    private static string FormatReport(FullAnalysisReport r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"==============================================================");
        sb.AppendLine($"ATLAS AUTONOMOUS FINANCE REPORT: {r.Symbol}");
        sb.AppendLine($"==============================================================");
        sb.AppendLine($"Generated At : {r.CurrentTime} (UTC {r.GeneratedAt:HH:mm})");
        sb.AppendLine($"Active Session: {r.ActiveSession} | Daily Bias: {r.SmartMoney?.DailyBias}");
        sb.AppendLine($"--------------------------------------------------------------");
        sb.AppendLine($"Current Price: ${r.CurrentPrice:N4} ({r.PriceChangePercent:+0.00;-0.00}%)");
        sb.AppendLine($"Open: ${r.OpenPrice:N4} | High: ${r.HighPrice:N4} | Low: ${r.LowPrice:N4} | Close: ${r.CurrentPrice:N4}");
        sb.AppendLine();
        sb.AppendLine($"[EXECUTIVE SUMMARY]");
        sb.AppendLine(r.ExecutiveSummary);
        sb.AppendLine();
        sb.AppendLine($"[MARKET STRUCTURE]");
        sb.AppendLine($"Trend Structure: {r.CurrentTrend} | Internal Wave: {r.SmartMoney?.MarketStructure?.InternalTrend}");
        sb.AppendLine($"Last Swing High: ${r.SmartMoney?.MarketStructure?.LastSwingHigh:N4} | Low: ${r.SmartMoney?.MarketStructure?.LastSwingLow:N4}");
        sb.AppendLine($"Equilibrium:     ${r.SmartMoney?.MarketStructure?.EquilibriumZone:N4}");
        sb.AppendLine($"Premium Area:    ${r.SmartMoney?.MarketStructure?.PremiumZone:N4} (Institutional Selling)");
        sb.AppendLine($"Discount Area:   ${r.SmartMoney?.MarketStructure?.DiscountZone:N4} (Institutional Buying)");
        sb.AppendLine($"Position:        {(r.SmartMoney?.MarketStructure?.IsInDiscount == true ? "DISCOUNT (Buy)" : r.SmartMoney?.MarketStructure?.IsInPremium == true ? "PREMIUM (Sell)" : "EQUILIBRIUM")}");
        sb.AppendLine();
        sb.AppendLine($"[SMART MONEY CONCEPTS & ICT]");
        sb.AppendLine($"Institutional Accumulation: {(r.SmartMoney?.InstitutionalAccumulation == true ? "ACTIVE 🟢" : "Inactive")}");
        sb.AppendLine($"Institutional Distribution: {(r.SmartMoney?.InstitutionalDistribution == true ? "ACTIVE 🔴" : "Inactive")}");
        sb.AppendLine($"Stop Hunt Detected:         {(r.SmartMoney?.StopHuntDetected == true ? $"Yes ({r.SmartMoney.StopHuntDirection})" : "No")}");
        sb.AppendLine($"OTE (61.8%-79%):            ${r.SmartMoney?.OTEZoneLow:N4}–${r.SmartMoney?.OTEZoneHigh:N4} (In Zone: {r.SmartMoney?.IsInOTE})");
        sb.AppendLine($"Judas Swing:                {(r.SmartMoney?.IsJudasSwing == true ? $"Yes ({r.SmartMoney.JudasDirection})" : "No")}");
        sb.AppendLine($"SMT Divergence:             {(r.SmartMoney?.SMTDivergence == true ? $"Yes ({r.SmartMoney.SMTDescription})" : "No")}");
        if (r.SmartMoney?.OrderBlocks?.Any() == true)
        {
            sb.AppendLine("Active Order Blocks:");
            foreach (var ob in r.SmartMoney.OrderBlocks.Take(3))
                sb.AppendLine($"  • [{ob.Zone}] range ${ob.Low:N4}-${ob.High:N4} (Strength {ob.Strength})");
        }
        if (r.SmartMoney?.FairValueGaps?.Any() == true)
        {
            sb.AppendLine("Imbalance Gaps (FVG):");
            foreach (var fvg in r.SmartMoney.FairValueGaps.Take(3))
                sb.AppendLine($"  • [{fvg.Type}] bounds ${fvg.Low:N4}-${fvg.High:N4} (Filled: {fvg.IsFilled})");
        }
        sb.AppendLine();
        sb.AppendLine($"[TECHNICAL INDICATORS]");
        sb.AppendLine($"Overall Signal:  {r.Indicators?.OverallSignal} ({r.Indicators?.BullishIndicators} Bull / {r.Indicators?.BearishIndicators} Bear)");
        sb.AppendLine($"RSI (14):        {r.Indicators?.RSI:F1} ({r.Indicators?.RSISignal})");
        sb.AppendLine($"MACD:            {r.Indicators?.MACD:F5} | Signal: {r.Indicators?.MACDSignal:F5} | Hist: {r.Indicators?.MACDHistogram:F5}");
        sb.AppendLine($"Moving Averages: EMA9=${r.Indicators?.EMA9:N4} | EMA20=${r.Indicators?.EMA20:N4} | EMA50=${r.Indicators?.EMA50:N4} | EMA200=${r.Indicators?.EMA200:N4}");
        sb.AppendLine($"Golden Cross:    {(r.Indicators?.GoldenCross == true ? "YES" : "NO")}");
        sb.AppendLine($"Bollinger Bands: Lower=${r.Indicators?.BollingerLower:N4} | Mid=${r.Indicators?.BollingerMiddle:N4} | Upper=${r.Indicators?.BollingerUpper:N4}");
        sb.AppendLine($"ADX:             {r.Indicators?.ADX:F1} ({r.Indicators?.ADXSignal}) | +DI={r.Indicators?.PlusDI:F1} | -DI={r.Indicators?.MinusDI:F1}");
        sb.AppendLine($"Stoch RSI:       {r.Indicators?.StochRSI:F1} ({r.Indicators?.StochRSISignal})");
        sb.AppendLine($"OBV:             {r.Indicators?.OBV:N0} ({r.Indicators?.OBVTrend})");
        sb.AppendLine($"CMF:             {r.Indicators?.CMF:F4} ({r.Indicators?.CMFSignal})");
        sb.AppendLine($"VWAP:            ${r.Indicators?.VWAP:N4} (Price {r.Indicators?.PriceVsVWAP} VWAP)");
        sb.AppendLine();
        if (r.CandlePatterns.Any())
        {
            sb.AppendLine($"[CANDLE PATTERNS DETECTED]");
            foreach (var p in r.CandlePatterns.Take(3))
                sb.AppendLine($"  • {p.Name} ({p.Type}) — {p.TradingImplication}");
            sb.AppendLine();
        }
        if (r.ChartPatterns.Any())
        {
            sb.AppendLine($"[CHART PATTERNS DETECTED]");
            foreach (var p in r.ChartPatterns.Take(2))
                sb.AppendLine($"  • {p.Name} ({p.Type}) Breakout level ${p.BreakoutLevel:N4} | Target ${p.TargetPrice:N4}");
            sb.AppendLine();
        }
        sb.AppendLine($"[NEWS VALIDATION]");
        sb.AppendLine($"Consensus Sentiment: {r.NewsSentiment}");
        sb.AppendLine(r.NewsImpact);
        if (r.News?.Headlines?.Any() == true)
        {
            sb.AppendLine("Recent Headlines:");
            foreach (var h in r.News.Headlines.Take(3))
                sb.AppendLine($"  • {h}");
        }
        sb.AppendLine();
        sb.AppendLine($"[MACRO OVERLAY]");
        sb.AppendLine(r.MacroAnalysis);
        sb.AppendLine();
        sb.AppendLine($"[RISK MANAGEMENT]");
        sb.AppendLine($"Volatility Risk Level: {r.RiskLevel} (Score: {r.RiskScore}/100)");
        sb.AppendLine($"Suggested Position:    {r.SuggestedPositionSize}% of portfolio");
        sb.AppendLine($"Optimal Entry Zone:    ${r.EntryZoneLow:N4}–${r.EntryZoneHigh:N4}");
        sb.AppendLine($"ATR-based Stop Loss:   ${r.StopLoss:N4} (Invalidation Point)");
        sb.AppendLine($"Take Profit Targets:   TP1: ${r.TakeProfit1:N4} | TP2: ${r.TakeProfit2:N4} | TP3: ${r.TakeProfit3:N4}");
        sb.AppendLine();
        sb.AppendLine($"[FORECAST & PROBABILITY]");
        sb.AppendLine($"Bullish Probability: {r.BullishProbability}% | Bearish: {r.BearishProbability}% | Neutral: {r.NeutralProbability}%");
        sb.AppendLine($"Model Confidence:    {r.ProbabilityScore}%");
        sb.AppendLine($"Bullish Case: {r.BullishScenario}");
        sb.AppendLine($"Bearish Case: {r.BearishScenario}");
        sb.AppendLine();
        sb.AppendLine($"[ACTIONABLE RECOMMENDATION]");
        sb.AppendLine($"RECOMMENDATION: {r.Recommendation.ToUpper()}");
        sb.AppendLine($"Confidence:     {r.OverallConfidence}%");
        sb.AppendLine();
        sb.AppendLine($"Key Reasons:");
        foreach (var re in r.Reasoning.Take(6))
            sb.AppendLine($"  • {re}");

        return sb.ToString();
    }
}