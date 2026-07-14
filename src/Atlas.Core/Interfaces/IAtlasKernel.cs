namespace Atlas.Core.Interfaces;

public interface IAtlasKernel
{
    Task<string> ProcessAsync(string input);
}