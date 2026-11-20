using System.Runtime.CompilerServices;
using Atlas.Core.Enums;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Atlas.Core.Services;

namespace Atlas.Tests;

public class ConversationStreamingTests
{
    private sealed class FakeAi : IAIProvider
    {
        public Task<string> GetResponseAsync(List<Message> messages) => Task.FromResult("whole reply");

        public async IAsyncEnumerable<string> StreamResponseAsync(
            List<Message> messages, [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (string piece in new[] { "Hello", ", ", "world." })
            {
                await Task.Yield();
                yield return piece;
            }
        }
    }

    private sealed class PlainAi : IAIProvider
    {
        public Task<string> GetResponseAsync(List<Message> messages) => Task.FromResult("whole reply");
    }

    private sealed class FakeStore : IConversationStore
    {
        public int Saves;
        public Conversation Create() => new();
        public void Save(Conversation conversation) => Saves++;
        public Conversation? Load(Guid id) => null;
        public void Delete(Guid id) { }
        public List<Conversation> GetAll() => new();
    }

    [Fact]
    public async Task Stream_YieldsChunksAndStoresTheJoinedReply()
    {
        FakeStore store = new();
        ConversationService service = new(new FakeAi(), store);

        List<string> chunks = new();
        await foreach (string chunk in service.StreamMessageAsync("hi"))
            chunks.Add(chunk);

        Assert.Equal(new[] { "Hello", ", ", "world." }, chunks);

        List<Message> messages = service.GetConversation().Messages;
        Assert.Equal(2, messages.Count);
        Assert.Equal(MessageRole.User, messages[0].Role);
        Assert.Equal("Hello, world.", messages[1].Content);
        Assert.Equal(MessageRole.Assistant, messages[1].Role);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task ProvidersThatCannotStream_FallBackToOneChunk()
    {
        ConversationService service = new(new PlainAi(), new FakeStore());

        List<string> chunks = new();
        await foreach (string chunk in service.StreamMessageAsync("hi"))
            chunks.Add(chunk);

        Assert.Equal(new[] { "whole reply" }, chunks);
    }

    [Fact]
    public async Task InterruptedStream_KeepsThePartialReply()
    {
        FakeStore store = new();
        ConversationService service = new(new FakeAi(), store);
        using CancellationTokenSource cts = new();

        await foreach (string chunk in service.StreamMessageAsync("hi", cts.Token))
            break; // stop after the first chunk, like a barge-in would

        Assert.Equal("Hello", service.GetConversation().Messages.Last().Content);
        Assert.Equal(1, store.Saves);
    }
}
