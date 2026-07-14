using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface IRiskEngine
{
    RiskAnalysis Analyze(TradingReport report);
}