using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface IPortfolioEngine
{
    PortfolioAnalysis Analyze(IEnumerable<TradingReport> assets);
}