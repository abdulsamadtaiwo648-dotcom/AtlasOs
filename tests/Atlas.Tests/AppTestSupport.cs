using System.Runtime.CompilerServices;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Atlas.Core.Services;
using Atlas.Core.Settings;
using Atlas.Speech.Loop;

namespace Atlas.Tests;

internal sealed class MemoryStore : IConversationStore
{
    private readonly Dictionary<Guid, Conversation> _items = new();

    public Conversation Create() => new();
    public void Save(Conversation conversation) => _items[conversation.Id] = conversation;
    public Conversation? Load(Guid id) => _items.GetValueOrDefault(id);
    public void Delete(Guid id) => _items.Remove(id);
    public List<Conversation> GetAll() => _items.Values.ToList();
}

internal sealed class ScriptedAi : IAIProvider
{
    public string[] Chunks = { "Hello", " there. ", "How can I help you today?" };
    public TimeSpan Delay = TimeSpan.Zero;

    /// <summary>Everything in the most recent prompt, joined, so tests can check what the model was told.</summary>
    public string LastPromptText { get; private set; } = "";

    public Task<string> GetResponseAsync(List<Message> messages)
    {
        LastPromptText = string.Join("\n", messages.Select(m => m.Content));
        return Task.FromResult(string.Concat(Chunks));
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        List<Message> messages, [EnumeratorCancellation] CancellationToken ct = default)
    {
        LastPromptText = string.Join("\n", messages.Select(m => m.Content));

        foreach (string chunk in Chunks)
        {
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, ct);
            ct.ThrowIfCancellationRequested();
            yield return chunk;
        }
    }
}

/// <summary>A kernel that just chats through the real conversation service.</summary>
internal sealed class ChatOnlyKernel : IAtlasKernel
{
    private readonly IConversationService _conversation;
    public Func<string, bool>? Fail;

    public ChatOnlyKernel(IConversationService conversation) => _conversation = conversation;

    public Task<string> ProcessAsync(string input) => _conversation.SendMessageAsync(input);

    public async IAsyncEnumerable<string> ProcessStreamAsync(
        string input, [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (Fail?.Invoke(input) == true)
            throw new InvalidOperationException("model exploded");

        await foreach (string chunk in _conversation.StreamMessageAsync(input, ct))
            yield return chunk;
    }
}

internal sealed class RecordingSpeaker : ISpeaker
{
    public List<string> Spoken = new();
    public Task SpeakAsync(string text, CancellationToken ct)
    {
        lock (Spoken) Spoken.Add(text);
        return Task.CompletedTask;
    }
}

internal sealed class FixedListener : IListener
{
    private readonly Queue<string> _lines;
    public FixedListener(params string[] lines) => _lines = new(lines);
    public Task<string> ListenAsync(CancellationToken ct) =>
        Task.FromResult(_lines.Count > 0 ? _lines.Dequeue() : "");
}

internal static class Wait
{
    public static async Task UntilAsync(Func<bool> condition, int timeoutMs = 3000)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > end)
                throw new TimeoutException("Condition was not met in time.");
            await Task.Delay(10);
        }
    }
}

internal sealed class TempSettings : IDisposable
{
    public string Path { get; } = global::System.IO.Path.Combine(
        global::System.IO.Path.GetTempPath(), $"atlas_settings_{Guid.NewGuid():N}.json");

    public JsonSettingsService Open() => new(Path);

    public void Dispose()
    {
        if (File.Exists(Path)) File.Delete(Path);
    }
}
