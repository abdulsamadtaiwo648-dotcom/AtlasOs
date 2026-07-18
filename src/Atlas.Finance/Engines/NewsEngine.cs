using Atlas.Finance.Interfaces;
using Atlas.Finance.Models;

namespace Atlas.Finance.Engines;

public class NewsEngine : INewsEngine
{
    private readonly IFinanceProvider _provider;

    public NewsEngine(IFinanceProvider provider)
    {
        _provider = provider;
    }

    public async Task<NewsAnalysis> AnalyzeNewsAsync(string asset)
    {
        return await _provider.GetNewsAsync(asset);
    }
}