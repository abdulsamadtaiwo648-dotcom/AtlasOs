using System.Runtime.CompilerServices;
using Atlas.Core.Models;

namespace Atlas.Core.Interfaces;

public interface IAIProvider
{
    Task<string> GetResponseAsync(List<Message> messages);

    /// <summary>
    /// Streams the reply as it is generated. Providers that cannot stream
    /// fall back to returning the whole reply as a single chunk.
    /// </summary>
    async IAsyncEnumerable<string> StreamResponseAsync(
        List<Message> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return await GetResponseAsync(messages);
    }
}