using Atlas.Core.Models;

namespace Atlas.Core.Interfaces;

public interface IConversationService
{
    Task<string> SendMessageAsync(string input);
    Conversation GetConversation();
}