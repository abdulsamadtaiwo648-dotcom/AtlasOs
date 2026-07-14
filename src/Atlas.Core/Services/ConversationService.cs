using Atlas.Core.Enums;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.Core.Services;

public class ConversationService : IConversationService
{
    private readonly IAIProvider _ai;
    private readonly IConversationStore _store;

    private readonly Conversation _conversation;

    public ConversationService(
        IAIProvider ai,
        IConversationStore store)
    {
        _ai = ai;
        _store = store;

        _conversation = _store.Create();
    }

    public async Task<string> SendMessageAsync(string input)
{
    Message userMessage = new()
    {
        ConversationId = _conversation.Id,
        Role = MessageRole.User,
        Content = input,
        Timestamp = DateTime.UtcNow
    };

    _conversation.Messages.Add(userMessage);

    string response = await _ai.GetResponseAsync(_conversation.Messages);

    Message assistantMessage = new()
    {
        ConversationId = _conversation.Id,
        Role = MessageRole.Assistant,
        Content = response,
        Timestamp = DateTime.UtcNow
    };

    _conversation.Messages.Add(assistantMessage);

    _store.Save(_conversation);

    return response;
}
    public Conversation GetConversation()
    {
        return _conversation;
    }
}