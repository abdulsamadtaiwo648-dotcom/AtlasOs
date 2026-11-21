using System.Runtime.CompilerServices;
using System.Text;
using Atlas.Core.Enums;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.Core.Services;

public class ConversationService : IConversationService
{
    private readonly IAIProvider _ai;
    private readonly IConversationStore _store;

    private readonly IMemoryService? _memory;

    private readonly Conversation _conversation;

    public ConversationService(
        IAIProvider ai,
        IConversationStore store,
        IMemoryService? memory = null)
    {
        _ai = ai;
        _store = store;
        _memory = memory;

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

    string response = await _ai.GetResponseAsync(BuildPrompt(input));

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
    public async IAsyncEnumerable<string> StreamMessageAsync(
        string input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _conversation.Messages.Add(new Message
        {
            ConversationId = _conversation.Id,
            Role = MessageRole.User,
            Content = input,
            Timestamp = DateTime.UtcNow
        });

        StringBuilder reply = new();

        try
        {
            await foreach (string chunk in _ai.StreamResponseAsync(
                BuildPrompt(input), cancellationToken))
            {
                reply.Append(chunk);
                yield return chunk;
            }
        }
        finally
        {
            // Keep whatever was generated, even if the turn was interrupted.
            if (reply.Length > 0)
            {
                _conversation.Messages.Add(new Message
                {
                    ConversationId = _conversation.Id,
                    Role = MessageRole.Assistant,
                    Content = reply.ToString(),
                    Timestamp = DateTime.UtcNow
                });

                _store.Save(_conversation);
            }
        }
    }

    /// <summary>
    /// The conversation so far, plus (for this request only) a note of the
    /// memories relevant to what was just said. The note is never saved in the
    /// conversation history.
    /// </summary>
    private List<Message> BuildPrompt(string input)
    {
        if (_memory == null)
            return _conversation.Messages;

        List<MemoryEntry> relevant = new();

        try
        {
            relevant.AddRange(_memory.Pinned());

            foreach (MemoryEntry entry in _memory.Search(input, 5))
            {
                if (relevant.All(r => r.Id != entry.Id))
                    relevant.Add(entry);
            }
        }
        catch (Exception)
        {
            // Memory is a bonus: if the database is unavailable, chat still works.
            return _conversation.Messages;
        }

        if (relevant.Count == 0)
            return _conversation.Messages;

        Message note = new()
        {
            ConversationId = _conversation.Id,
            Role = MessageRole.System,
            Content =
                "Things the user has told you to remember. Use them naturally when relevant; " +
                "do not recite them or mention that you have a memory list:\n" +
                string.Join("\n", relevant.Select(m => $"- {m.Content}"))
        };

        // The note goes first so the conversation (ending with the user's message) stays last.
        List<Message> prompt = new() { note };
        prompt.AddRange(_conversation.Messages);
        return prompt;
    }

    public Conversation GetConversation()
    {
        return _conversation;
    }
}