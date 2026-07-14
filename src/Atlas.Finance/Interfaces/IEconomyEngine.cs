using Atlas.Finance.Models;
namespace Atlas.Finance.Interfaces;

public interface IEconomyEngine
{
    Task<EconomicSummary> GetSummaryAsync();

    // Task<EconomicSummary> GetCountrySummaryAsync(string country);

    // Task<EconomicSummary> AnalyzeAsync(string query);
}