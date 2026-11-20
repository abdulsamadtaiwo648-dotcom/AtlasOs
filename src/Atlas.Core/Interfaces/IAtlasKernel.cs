namespace Atlas.Core.Interfaces;

public interface IAtlasKernel
{
    Task<string> ProcessAsync(string input);

    /// <summary>
    /// Same as <see cref="ProcessAsync"/>, but yields the reply in pieces as it is
    /// produced so a caller (for example the voice loop) can start speaking early.
    /// </summary>
    IAsyncEnumerable<string> ProcessStreamAsync(string input, CancellationToken cancellationToken = default);
}