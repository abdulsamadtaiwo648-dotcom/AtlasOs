using Atlas.Core.Models;

namespace Atlas.Core.Interfaces;

public interface IConversationService
{
    Task<string> SendMessageAsync(string input);

    IAsyncEnumerable<string> StreamMessageAsync(string input, CancellationToken cancellationToken = default);
    Conversation GetConversation();

    /// <summary>Begins a fresh conversation (the old one stays saved).</summary>
    Conversation StartNew();

    /// <summary>Switches to a saved conversation. Returns false if it doesn't exist.</summary>
    bool Load(Guid id);
}