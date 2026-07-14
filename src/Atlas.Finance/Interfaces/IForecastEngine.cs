using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface IForecastEngine
{
    ForecastResult Predict(TradingReport report);
}