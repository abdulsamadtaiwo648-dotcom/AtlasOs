using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Atlas.App.Mvvm;
using Atlas.Core.Enums;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Atlas.Core.Settings;
using Atlas.Speech.Loop;

namespace Atlas.App.Chat;

/// <summary>The chat screen: messages, streaming replies, history and the input box.</summary>
public sealed class ChatViewModel : ObservableObject
{
    private readonly IAtlasKernel _kernel;
    private readonly IConversationService _conversation;
    private readonly IConversationStore _store;
    private readonly ISettingsService _settings;
    private readonly ISpeaker? _speaker;
    private readonly IUiDispatcher _ui;

    private string _inputText = "";
    private bool _isBusy;
    private string _title = "New chat";
    private CancellationTokenSource? _cts;

    public ChatViewModel(
        IAtlasKernel kernel,
        IConversationService conversation,
        IConversationStore store,
        ISettingsService settings,
        IUiDispatcher ui,
        ISpeaker? speaker = null)
    {
        _kernel = kernel;
        _conversation = conversation;
        _store = store;
        _settings = settings;
        _ui = ui;
        _speaker = speaker;

        SendCommand = new AsyncRelayCommand(SendAsync, () => CanSend, ex => ShowError(ex.Message));
        StopCommand = new RelayCommand(Stop, () => IsBusy);
        NewChatCommand = new RelayCommand(NewChat);
        OpenConversationCommand = new RelayCommand(p => { if (p is ConversationSummary s) Open(s.Id); });
        UseSuggestionCommand = new RelayCommand(p => { if (p is string text) InputText = text; });
        DeleteConversationCommand = new RelayCommand(p => { if (p is ConversationSummary s) Delete(s.Id); });

        ShowConversation();
        RefreshHistory();
    }

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();
    public ObservableCollection<ConversationSummary> History { get; } = new();

    public AsyncRelayCommand SendCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand NewChatCommand { get; }
    public RelayCommand OpenConversationCommand { get; }
    public RelayCommand DeleteConversationCommand { get; }
    public RelayCommand UseSuggestionCommand { get; }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (Set(ref _inputText, value))
                SendCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!Set(ref _isBusy, value))
                return;

            OnPropertyChanged(nameof(CanSend));
            SendCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }

    public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    public string Title
    {
        get => _title;
        private set => Set(ref _title, value);
    }

    public bool IsEmpty => Messages.Count == 0;

    // ── Sending ────────────────────────────────────────────────────────────

    private async Task SendAsync()
    {
        string text = InputText.Trim();
        if (text.Length == 0)
            return;

        InputText = "";
        IsBusy = true;
        _cts = new CancellationTokenSource();

        try
        {
            AtlasSettings settings = _settings.Current;
            bool speak = settings.SpeakReplies && _speaker != null;
            SentenceChunker chunker = new();
            Task speaking = Task.CompletedTask;

            Task Enqueue(Task previous, string sentence) =>
                previous.ContinueWith(
                    _ => _speaker!.SpeakAsync(sentence, _cts.Token),
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default).Unwrap();

            await foreach (string chunk in RespondAsync(text, _cts.Token))
            {
                if (!speak)
                    continue;

                foreach (string sentence in chunker.Push(chunk))
                    speaking = Enqueue(speaking, sentence);
            }

            if (speak)
            {
                foreach (string rest in chunker.Flush())
                    speaking = Enqueue(speaking, rest);

                try { await speaking; } catch (OperationCanceledException) { }
            }
        }
        catch (OperationCanceledException)
        {
            // Stop was pressed: keep what was already written.
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Shows the user's message and streams Atlas's reply into the chat, yielding each
    /// piece as it arrives. Used by typed chat and by the voice loop, so both look the same.
    /// </summary>
    public async IAsyncEnumerable<string> RespondAsync(
        string userText,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ChatMessageViewModel user = new(ChatRole.User, userText);
        ChatMessageViewModel reply = new(ChatRole.Assistant, "") { IsStreaming = true };

        _ui.Post(() =>
        {
            Messages.Add(user);
            Messages.Add(reply);
            OnPropertyChanged(nameof(IsEmpty));
        });

        try
        {
            await foreach (string chunk in _kernel.ProcessStreamAsync(userText, cancellationToken))
            {
                _ui.Post(() => reply.Text += chunk);
                yield return chunk;
            }
        }
        finally
        {
            _ui.Post(() =>
            {
                reply.IsStreaming = false;

                if (reply.Text.Length == 0)
                    Messages.Remove(reply);

                OnPropertyChanged(nameof(IsEmpty));
                RefreshHistory();
            });
        }
    }

    public void Stop() => _cts?.Cancel();

    // ── Conversations ──────────────────────────────────────────────────────

    public void NewChat()
    {
        if (IsBusy)
            return;

        _conversation.StartNew();
        ShowConversation();
        RefreshHistory();
    }

    public void Open(Guid id)
    {
        if (IsBusy || !_conversation.Load(id))
            return;

        ShowConversation();
    }

    public void Delete(Guid id)
    {
        if (IsBusy)
            return;

        bool wasCurrent = _conversation.GetConversation().Id == id;
        _store.Delete(id);

        if (wasCurrent)
        {
            _conversation.StartNew();
            ShowConversation();
        }

        RefreshHistory();
    }

    public void RefreshHistory()
    {
        Guid current = _conversation.GetConversation().Id;

        List<ConversationSummary> items = _store.GetAll()
            .Where(c => c.Messages.Any(m => m.Role == MessageRole.User))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ConversationSummary(c.Id, TitleOf(c), c.CreatedAt))
            .ToList();

        History.Clear();
        foreach (ConversationSummary item in items)
            History.Add(item);

        Title = items.FirstOrDefault(i => i.Id == current)?.Title ?? "New chat";
    }

    private void ShowConversation()
    {
        Messages.Clear();

        foreach (Message message in _conversation.GetConversation().Messages)
        {
            if (message.Role == MessageRole.System)
                continue;

            Messages.Add(new ChatMessageViewModel(
                message.Role == MessageRole.User ? ChatRole.User : ChatRole.Assistant,
                message.Content,
                message.Timestamp.ToLocalTime()));
        }

        OnPropertyChanged(nameof(IsEmpty));
        Title = "New chat";
        RefreshHistoryTitle();
    }

    private void RefreshHistoryTitle()
    {
        Title = TitleOf(_conversation.GetConversation());
    }

    internal static string TitleOf(Conversation conversation) => conversation.Title();

    private void ShowError(string message)
    {
        _ui.Post(() =>
        {
            Messages.Add(new ChatMessageViewModel(ChatRole.System, message));
            OnPropertyChanged(nameof(IsEmpty));
        });
    }
}
