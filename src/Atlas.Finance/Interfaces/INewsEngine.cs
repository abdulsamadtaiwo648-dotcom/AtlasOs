using Atlas.Finance.Models;

namespace Atlas.Finance.Interfaces;

public interface INewsEngine
{
    Task<NewsAnalysis> AnalyzeNewsAsync(string asset);
}