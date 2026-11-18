using Atlas.Core.Models;

namespace Atlas.Core.Interfaces;

public interface IConversationService
{
    Task<string> SendMessageAsync(string input);

    IAsyncEnumerable<string> StreamMessageAsync(string input, CancellationToken cancellationToken = default);
    Conversation GetConversation();
}