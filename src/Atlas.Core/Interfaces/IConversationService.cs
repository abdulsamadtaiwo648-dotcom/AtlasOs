using Atlas.Core.Models;

namespace Atlas.Core.Interfaces;

public interface IConversationService
{
    Task<string> SendMessageAsync(string input);

    IAsyncEnumerable<string> StreamMessageAsync(string input, CancellationToken cancellationToken = default);
    Conversation GetConversation();

    /// <summary>
    /// Saves an exchange that Atlas answered itself (calculator, smart home, memory...) so it
    /// appears in the chat history and gives later AI replies some context.
    /// </summary>
    void RecordExchange(string userText, string reply);

    /// <summary>Begins a fresh conversation (the old one stays saved).</summary>
    Conversation StartNew();

    /// <summary>Switches to a saved conversation. Returns false if it doesn't exist.</summary>
    bool Load(Guid id);
}