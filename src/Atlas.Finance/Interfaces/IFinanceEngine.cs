namespace Atlas.Finance.Interfaces;

public interface IFinanceEngine
{
    Task<string> BuildContextAsync(string input);
}