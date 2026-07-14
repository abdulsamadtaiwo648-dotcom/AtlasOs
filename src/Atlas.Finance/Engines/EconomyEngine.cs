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
        // TODO: Replace with live API later.
        return new EconomicSummary
        {
            Country = "Global",
            InflationRate = 0,
            InterestRate = 0,
            GdpGrowth = 0,
            UnemploymentRate = 0,
            Outlook = "Unavailable"
        };
    }
    public async Task<EconomicSummary> GetSummaryAsync()
    {
        throw new NotImplementedException();
    }
    //  public async Task<EconomicSummary> GetCo()
    // {
    //    throw new NotImplementedException();
    // }
}

