using Atlas.App;
using Atlas.App.Chat;
using Atlas.App.Memory;
using Atlas.App.Mvvm;
using Atlas.App.Settings;
using Atlas.App.Voice;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Atlas.Core.Services;
using Atlas.Core.Settings;
using Atlas.Speech.Loop;

namespace Atlas.Tests;

public class ChatViewModelTests
{
    private sealed class Rig : IDisposable
    {
        public readonly TempSettings Temp = new();
        public readonly MemoryStore Store = new();
        public readonly ScriptedAi Ai = new();
        public readonly ConversationService Conversation;
        public readonly ChatOnlyKernel Kernel;
        public readonly JsonSettingsService Settings;
        public readonly RecordingSpeaker Speaker = new();
        public readonly ChatViewModel Chat;

        public Rig()
        {
            Settings = Temp.Open();
            Conversation = new ConversationService(Ai, Store);
            Kernel = new ChatOnlyKernel(Conversation);
            Chat = new ChatViewModel(Kernel, Conversation, Store, Settings, new ImmediateDispatcher(), Speaker);
        }

        public void Dispose() => Temp.Dispose();
    }

    [Fact]
    public async Task Send_ShowsUserMessage_StreamsReply_AndClearsInput()
    {
        using Rig rig = new();
        rig.Chat.InputText = "hi atlas";

        await rig.Chat.SendCommand.ExecuteAsync();

        Assert.Equal("", rig.Chat.InputText);
        Assert.Equal(2, rig.Chat.Messages.Count);
        Assert.True(rig.Chat.Messages[0].IsUser);
        Assert.Equal("hi atlas", rig.Chat.Messages[0].Text);
        Assert.True(rig.Chat.Messages[1].IsAssistant);
        Assert.Equal("Hello there. How can I help you today?", rig.Chat.Messages[1].Text);
        Assert.False(rig.Chat.Messages[1].IsStreaming);
        Assert.False(rig.Chat.IsBusy);
        Assert.False(rig.Chat.IsEmpty);
    }

    [Fact]
    public void CannotSendEmptyText()
    {
        using Rig rig = new();

        rig.Chat.InputText = "   ";
        Assert.False(rig.Chat.SendCommand.CanExecute(null));

        rig.Chat.InputText = "hello";
        Assert.True(rig.Chat.SendCommand.CanExecute(null));
    }

    [Fact]
    public async Task Send_RecordsTheChatInHistoryWithATitle()
    {
        using Rig rig = new();
        rig.Chat.InputText = "What is the capital of France and why is it so famous worldwide?";

        await rig.Chat.SendCommand.ExecuteAsync();

        ConversationSummary item = Assert.Single(rig.Chat.History);
        Assert.StartsWith("What is the capital of France", item.Title);
        Assert.EndsWith("...", item.Title);
        Assert.True(item.Title.Length <= 40);
        Assert.Equal(item.Title, rig.Chat.Title);
    }

    [Fact]
    public async Task NewChat_ClearsTheScreen_AndOldChatCanBeReopenedOrDeleted()
    {
        using Rig rig = new();
        rig.Chat.InputText = "first question";
        await rig.Chat.SendCommand.ExecuteAsync();
        Guid firstId = Assert.Single(rig.Chat.History).Id;

        rig.Chat.NewChat();
        Assert.True(rig.Chat.IsEmpty);
        Assert.Equal("New chat", rig.Chat.Title);

        rig.Chat.OpenConversationCommand.Execute(rig.Chat.History.Single());
        Assert.Equal(2, rig.Chat.Messages.Count);
        Assert.Equal("first question", rig.Chat.Messages[0].Text);

        rig.Chat.DeleteConversationCommand.Execute(rig.Chat.History.Single());
        Assert.Empty(rig.Chat.History);
        Assert.True(rig.Chat.IsEmpty);
        Assert.Null(rig.Store.Load(firstId));
    }

    [Fact]
    public async Task Stop_KeepsThePartOfTheReplyAlreadyWritten()
    {
        using Rig rig = new();
        rig.Ai.Delay = TimeSpan.FromMilliseconds(60);
        rig.Chat.InputText = "tell me a long story";

        Task send = rig.Chat.SendCommand.ExecuteAsync();
        await Wait.UntilAsync(() => rig.Chat.Messages.Count == 2 && rig.Chat.Messages[1].Text.Length > 0);

        rig.Chat.Stop();
        await send;

        string partial = rig.Chat.Messages[1].Text;
        Assert.NotEmpty(partial);
        Assert.False(partial.EndsWith("today?"));
        Assert.False(rig.Chat.Messages[1].IsStreaming);
        Assert.False(rig.Chat.IsBusy);
    }

    [Fact]
    public async Task ErrorsAreShownInTheChatInsteadOfCrashing()
    {
        using Rig rig = new();
        rig.Kernel.Fail = _ => true;
        rig.Chat.InputText = "break please";

        rig.Chat.SendCommand.Execute(null);
        await Wait.UntilAsync(() => rig.Chat.Messages.Any(m => m.IsSystem));

        Assert.Contains("model exploded", rig.Chat.Messages.First(m => m.IsSystem).Text);
        Assert.False(rig.Chat.IsBusy);
    }

    [Fact]
    public async Task RepliesAreSpokenOnlyWhenTheSettingIsOn()
    {
        using Rig rig = new();

        rig.Chat.InputText = "quiet please";
        await rig.Chat.SendCommand.ExecuteAsync();
        Assert.Empty(rig.Speaker.Spoken);

        rig.Settings.Update(s => s.SpeakReplies = true);
        rig.Chat.InputText = "now speak";
        await rig.Chat.SendCommand.ExecuteAsync();

        // "Hello there." is shorter than the chunker's minimum, so it is spoken together with what follows.
        Assert.Equal(new[] { "Hello there. How can I help you today?" }, rig.Speaker.Spoken);
    }

    [Fact]
    public async Task Respond_IsUsableByTheVoiceLoop_AndShowsBothSidesInChat()
    {
        using Rig rig = new();

        List<string> chunks = new();
        await foreach (string chunk in rig.Chat.RespondAsync("said out loud"))
            chunks.Add(chunk);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("said out loud", rig.Chat.Messages[0].Text);
        Assert.Equal("Hello there. How can I help you today?", rig.Chat.Messages[1].Text);
    }

    [Fact]
    public async Task ReopeningAChatShowsItsMessagesButNotInternalSystemNotes()
    {
        using Rig rig = new();
        rig.Conversation.GetConversation().Messages.Add(new Message { Role = Atlas.Core.Enums.MessageRole.System, Content = "hidden" });
        rig.Chat.InputText = "visible question";
        await rig.Chat.SendCommand.ExecuteAsync();

        rig.Chat.NewChat();
        rig.Chat.Open(rig.Chat.History.Single().Id);

        Assert.DoesNotContain(rig.Chat.Messages, m => m.Text == "hidden");
        Assert.Equal(2, rig.Chat.Messages.Count);
    }
}

public class MemoryViewModelTests
{
    [Fact]
    public void AddSearchPinAndDelete()
    {
        using TempDb db = new();
        MemoryViewModel vm = new(db.Open());
        Assert.True(vm.IsEmpty);
        Assert.Contains("remember", vm.EmptyText);

        vm.NewMemoryText = "My sister is called Ola";
        Assert.True(vm.AddCommand.CanExecute(null));
        vm.AddCommand.Execute(null);
        vm.NewMemoryText = "I like jollof rice";
        vm.AddCommand.Execute(null);

        Assert.Equal(2, vm.Items.Count);
        Assert.Equal("2 memories", vm.CountText);
        Assert.Equal("", vm.NewMemoryText);

        vm.SearchText = "sister";
        Assert.Single(vm.Items);
        Assert.Equal("My sister is called Ola", vm.Items[0].Content);

        vm.SearchText = "zzzz";
        Assert.True(vm.IsEmpty);
        Assert.Contains("matches", vm.EmptyText);

        vm.ClearSearchCommand.Execute(null);
        MemoryItemViewModel jollof = vm.Items.First(i => i.Content.Contains("jollof"));
        vm.TogglePinCommand.Execute(jollof);
        Assert.True(vm.Items.First().IsPinned);
        Assert.Equal("I like jollof rice", vm.Items.First().Content);

        vm.TogglePinCommand.Execute(vm.Items.First());
        Assert.DoesNotContain(vm.Items, i => i.IsPinned);

        vm.DeleteCommand.Execute(vm.Items.First(i => i.Content.Contains("sister")));
        Assert.Single(vm.Items);
        Assert.Equal("1 memory", vm.CountText);
    }

    [Fact]
    public void AddingTheSameThingTwice_SaysSo_AndAddsNothing()
    {
        using TempDb db = new();
        MemoryViewModel vm = new(db.Open());

        vm.NewMemoryText = "I live in Lagos";
        vm.AddCommand.Execute(null);
        vm.NewMemoryText = "i live in lagos";
        vm.AddCommand.Execute(null);

        Assert.Single(vm.Items);
        Assert.Contains("already knew", vm.Status);
    }

    [Fact]
    public void Blank_CannotBeAdded()
    {
        using TempDb db = new();
        MemoryViewModel vm = new(db.Open());

        vm.NewMemoryText = "  ";

        Assert.False(vm.AddCommand.CanExecute(null));
    }
}

public class SettingsViewModelTests
{
    private sealed class FakeOllama : IOllamaClient
    {
        public string[] Models = { "gemma3:4b", "qwen3:8b" };
        public bool Fail;

        public Task<IReadOnlyList<string>> ListModelsAsync(string baseUrl, CancellationToken ct)
        {
            if (Fail) throw new HttpRequestException("refused");
            return Task.FromResult<IReadOnlyList<string>>(Models);
        }
    }

    [Fact]
    public void EditingAField_SavesImmediately()
    {
        using TempSettings temp = new();
        JsonSettingsService service = temp.Open();
        SettingsViewModel vm = new(service, new FakeOllama());

        vm.Model = "qwen3:8b";
        vm.SpeakReplies = true;
        vm.SpeechSpeed = 1.3;
        vm.StartHandsFree = true;

        Assert.Equal("qwen3:8b", service.Current.Model);
        Assert.True(service.Current.SpeakReplies);
        Assert.Equal(1.3, service.Current.SpeechSpeed);
        Assert.True(service.Current.StartHandsFree);
        Assert.Equal("1.3x", vm.SpeechSpeedText);
    }

    [Fact]
    public async Task CheckConnection_ListsInstalledModels()
    {
        using TempSettings temp = new();
        SettingsViewModel vm = new(temp.Open(), new FakeOllama());

        await vm.CheckConnectionAsync();

        Assert.Equal(new[] { "gemma3:4b", "qwen3:8b" }, vm.AvailableModels);
        Assert.StartsWith("Connected.", vm.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnection_WarnsWhenTheChosenModelIsNotInstalled()
    {
        using TempSettings temp = new();
        SettingsViewModel vm = new(temp.Open(), new FakeOllama()) { Model = "llama9:70b" };

        await vm.CheckConnectionAsync();

        Assert.Contains("isn't installed", vm.ConnectionStatus);
        Assert.Contains("ollama pull llama9:70b", vm.ConnectionStatus);
    }

    [Fact]
    public async Task CheckConnection_ExplainsWhenOllamaIsNotRunning()
    {
        using TempSettings temp = new();
        SettingsViewModel vm = new(temp.Open(), new FakeOllama { Fail = true });

        await vm.CheckConnectionAsync();

        Assert.Contains("Is it running?", vm.ConnectionStatus);
    }

    [Fact]
    public void ReportsMissingVoiceModels()
    {
        using TempSettings temp = new();
        string previous = Environment.GetEnvironmentVariable("ATLAS_HOME") ?? "";

        SettingsViewModel vm = new(temp.Open(), new FakeOllama());

        // On a machine without the models (like CI) voice is reported as not ready.
        Assert.Equal(vm.SpeechReady, vm.SpeechStatus == "Voice models found.");
    }
}

public class VoiceViewModelTests
{
    private static (VoiceViewModel voice, ChatViewModel chat, RecordingSpeaker speaker, TempSettings temp) Build(
        IListener listener, Func<IReadOnlyList<string>> missing)
    {
        TempSettings temp = new();
        MemoryStore store = new();
        ConversationService conversation = new(new ScriptedAi(), store);
        RecordingSpeaker speaker = new();
        ChatViewModel chat = new(new ChatOnlyKernel(conversation), conversation, store, temp.Open(), new ImmediateDispatcher());
        VoiceViewModel voice = new(new TriggerableWakeSource(), listener, speaker, chat, new ImmediateDispatcher(), missing);
        return (voice, chat, speaker, temp);
    }

    [Fact]
    public void WontStart_WhenVoiceModelsAreMissing_AndSaysWhy()
    {
        (VoiceViewModel voice, _, _, TempSettings temp) = Build(new FixedListener(), () => new[] { "model.onnx" });
        using (temp)
        {
            voice.Start();

            Assert.False(voice.IsHandsFree);
            Assert.Contains("1 model file(s) missing", voice.Message);
        }
    }

    [Fact]
    public async Task ClickToTalk_RunsAWholeTurn_AndShowsItInTheChat()
    {
        (VoiceViewModel voice, ChatViewModel chat, RecordingSpeaker speaker, TempSettings temp) =
            Build(new FixedListener("what time is it"), () => Array.Empty<string>());
        using (temp)
        {
            voice.Talk();
            Assert.True(voice.IsHandsFree);

            await Wait.UntilAsync(() => speaker.Spoken.Count >= 1);

            Assert.Equal("what time is it", chat.Messages[0].Text);
            Assert.Equal("Hello there. How can I help you today?", chat.Messages[1].Text);

            await voice.StopAsync();
            await Wait.UntilAsync(() => !voice.IsHandsFree);
            Assert.Equal(VoiceState.Stopped, voice.State);
            Assert.Equal("Voice is off", voice.StatusText);
        }
    }

    [Fact]
    public async Task Toggle_StartsAndStopsTheHandsFreeLoop()
    {
        (VoiceViewModel voice, _, _, TempSettings temp) = Build(new FixedListener(), () => Array.Empty<string>());
        using (temp)
        {
            voice.ToggleHandsFreeCommand.Execute(null);
            Assert.True(voice.IsHandsFree);
            await Wait.UntilAsync(() => voice.State == VoiceState.Idle);

            voice.ToggleHandsFreeCommand.Execute(null);
            await Wait.UntilAsync(() => !voice.IsHandsFree);
        }
    }
}

public class ShellViewModelTests
{
    [Fact]
    public async Task Navigation_UpdatesFlags_AndRefreshesMemory()
    {
        using TempDb db = new();
        using TempSettings temp = new();
        JsonSettingsService settings = temp.Open();
        MemoryStore store = new();
        ConversationService conversation = new(new ScriptedAi(), store);
        ChatViewModel chat = new(new ChatOnlyKernel(conversation), conversation, store, settings, new ImmediateDispatcher());
        SqliteMemoryServiceAlias memory = new(db);
        MemoryViewModel memoryVm = new(memory.Service);
        SettingsViewModel settingsVm = new(settings, new NullOllama());
        VoiceViewModel voice = new(new TriggerableWakeSource(), new FixedListener(), new RecordingSpeaker(), chat,
            new ImmediateDispatcher(), () => new[] { "x" });
        ShellViewModel shell = new(chat, memoryVm, settingsVm, voice, settings);

        Assert.True(shell.IsChatPage);

        memory.Service.Remember("added behind the UI's back");
        shell.ShowMemoryCommand.Execute(null);

        Assert.True(shell.IsMemoryPage);
        Assert.False(shell.IsChatPage);
        Assert.Single(memoryVm.Items);

        shell.ShowSettingsCommand.Execute(null);
        Assert.True(shell.IsSettingsPage);

        shell.ShowChatCommand.Execute(null);
        Assert.True(shell.IsChatPage);

        await shell.OnClosingAsync();
    }

    private sealed class NullOllama : IOllamaClient
    {
        public Task<IReadOnlyList<string>> ListModelsAsync(string baseUrl, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    private sealed class SqliteMemoryServiceAlias
    {
        public Atlas.Memory.SqliteMemoryService Service { get; }
        public SqliteMemoryServiceAlias(TempDb db) => Service = db.Open();
    }
}
