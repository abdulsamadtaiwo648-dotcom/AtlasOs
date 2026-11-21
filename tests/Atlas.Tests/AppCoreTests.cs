using Atlas.Core.Commands;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Atlas.Core.Services;
using Atlas.Core.Settings;
using Atlas.Speech.Loop;

namespace Atlas.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void MissingFile_GivesDefaults()
    {
        using TempSettings temp = new();
        AtlasSettings s = temp.Open().Current;

        Assert.Equal("http://127.0.0.1:11434", s.OllamaUrl);
        Assert.Equal("gemma3:4b", s.Model);
        Assert.False(s.SpeakReplies);
        Assert.Equal(1.0, s.SpeechSpeed);
    }

    [Fact]
    public void Update_IsSavedAndReloaded_AndRaisesChanged()
    {
        using TempSettings temp = new();
        JsonSettingsService service = temp.Open();
        int changes = 0;
        service.Changed += () => changes++;

        service.Update(s => { s.Model = "qwen3:8b"; s.SpeakReplies = true; });

        Assert.Equal(1, changes);
        AtlasSettings reloaded = temp.Open().Current;
        Assert.Equal("qwen3:8b", reloaded.Model);
        Assert.True(reloaded.SpeakReplies);
    }

    [Fact]
    public void Update_CleansUpBadValues()
    {
        using TempSettings temp = new();
        JsonSettingsService service = temp.Open();

        service.Update(s => { s.OllamaUrl = "  http://localhost:11434/  "; s.Model = "   "; s.SpeechSpeed = 9; });

        Assert.Equal("http://localhost:11434", service.Current.OllamaUrl);
        Assert.Equal("gemma3:4b", service.Current.Model);
        Assert.Equal(2.0, service.Current.SpeechSpeed);
    }

    [Fact]
    public void CorruptFile_FallsBackToDefaultsInsteadOfCrashing()
    {
        using TempSettings temp = new();
        File.WriteAllText(temp.Path, "{ this is not json");

        Assert.Equal("gemma3:4b", temp.Open().Current.Model);
    }
}

public class ConversationSwitchingTests
{
    [Fact]
    public async Task StartNew_BeginsAnEmptyConversation_AndLoadGoesBack()
    {
        MemoryStore store = new();
        ConversationService service = new(new ScriptedAi(), store);

        await service.SendMessageAsync("first chat");
        Guid first = service.GetConversation().Id;

        service.StartNew();
        Assert.NotEqual(first, service.GetConversation().Id);
        Assert.Empty(service.GetConversation().Messages);

        Assert.True(service.Load(first));
        Assert.Equal(first, service.GetConversation().Id);
        Assert.Equal(2, service.GetConversation().Messages.Count);

        Assert.False(service.Load(Guid.NewGuid()));
    }

    [Fact]
    public void NewCommand_ReallyStartsANewConversation()
    {
        ConversationService service = new(new ScriptedAi(), new MemoryStore());
        Guid before = service.GetConversation().Id;

        new NewCommand(service).Execute(Array.Empty<string>());

        Assert.NotEqual(before, service.GetConversation().Id);
    }
}

public class TriggerableWakeSourceTests
{
    private sealed class ManualWake : IWakeSource
    {
        private readonly TaskCompletionSource _tcs = new();
        public void Fire() => _tcs.TrySetResult();
        public Task WaitForWakeAsync(CancellationToken ct) => _tcs.Task.WaitAsync(ct);
    }

    [Fact]
    public async Task Trigger_WakesAWaitingLoop()
    {
        TriggerableWakeSource source = new();
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));

        Task wait = source.WaitForWakeAsync(cts.Token);
        Assert.False(wait.IsCompleted);

        source.Trigger();
        await wait;
    }

    [Fact]
    public async Task TriggerBeforeWaiting_IsNotLost_ButOnlyUsedOnce()
    {
        TriggerableWakeSource source = new();
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));

        source.Trigger();
        await source.WaitForWakeAsync(cts.Token);

        using CancellationTokenSource shortWait = new(TimeSpan.FromMilliseconds(150));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.WaitForWakeAsync(shortWait.Token));
    }

    [Fact]
    public async Task WakeWord_AlsoWakesIt()
    {
        ManualWake inner = new();
        TriggerableWakeSource source = new(inner);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));

        Task wait = source.WaitForWakeAsync(cts.Token);
        inner.Fire();
        await wait;
    }

    [Fact]
    public async Task Cancelling_StopsWaiting()
    {
        TriggerableWakeSource source = new(new ManualWake());
        using CancellationTokenSource cts = new();

        Task wait = source.WaitForWakeAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }
}
