namespace Atlas.Core.Interfaces;

using Atlas.Core.Models;

public interface IAIProvider
{
    Task<string> GetResponseAsync(List<Message> messages);
}