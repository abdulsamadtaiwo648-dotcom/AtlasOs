using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;

namespace Atlas.Finance.Engines;

public class EconomyEngine : IEconomyEngine
{
    private readonly IFinanceProvider _provider;

    public EconomyEngine(IFinanceProvider provider)
    {
        _provider = provider;
    }

    public async Task<EconomicSummary> GetEconomicSummaryAsync()
    {
        try
        {
            return await _provider.GetEconomicSummaryAsync();
        }
        catch
        {
            return new EconomicSummary
            {
                Country          = "Global",
                InflationRate    = 2.5m,
                InterestRate     = 4.5m,
                GdpGrowth        = 2.1m,
                UnemploymentRate = 3.8m,
                Outlook          = "Stable"
            };
        }
    }

    public async Task<EconomicSummary> GetSummaryAsync()
    {
        return await GetEconomicSummaryAsync();
    }
}
