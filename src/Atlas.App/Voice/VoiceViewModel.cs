using Atlas.App.Chat;
using Atlas.App.Mvvm;
using Atlas.Speech;
using Atlas.Speech.Loop;

namespace Atlas.App.Voice;

/// <summary>The voice orb: runs the hands-free loop and shows what it is doing.</summary>
public sealed class VoiceViewModel : ObservableObject
{
    private readonly TriggerableWakeSource _wake;
    private readonly IListener _listener;
    private readonly ISpeaker _speaker;
    private readonly ChatViewModel _chat;
    private readonly IUiDispatcher _ui;
    private readonly Func<IReadOnlyList<string>> _missingFiles;

    private CancellationTokenSource? _cts;
    private Task _running = Task.CompletedTask;
    private VoiceState _state = VoiceState.Stopped;
    private bool _isHandsFree;
    private string _message = "";

    public VoiceViewModel(
        TriggerableWakeSource wake,
        IListener listener,
        ISpeaker speaker,
        ChatViewModel chat,
        IUiDispatcher ui,
        Func<IReadOnlyList<string>>? missingFiles = null)
    {
        _wake = wake;
        _listener = listener;
        _speaker = speaker;
        _chat = chat;
        _ui = ui;
        _missingFiles = missingFiles ?? SpeechPaths.MissingModelFiles;

        ToggleHandsFreeCommand = new RelayCommand(() => { if (IsHandsFree) Stop(); else Start(); });
        TalkCommand = new RelayCommand(Talk);
    }

    public RelayCommand ToggleHandsFreeCommand { get; }
    public RelayCommand TalkCommand { get; }

    public VoiceState State
    {
        get => _state;
        private set
        {
            if (!Set(ref _state, value))
                return;

            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsActive));
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(IsListening));
            OnPropertyChanged(nameof(IsThinking));
            OnPropertyChanged(nameof(IsSpeaking));
        }
    }

    public bool IsActive => State != VoiceState.Stopped;
    public bool IsIdle => State == VoiceState.Idle;
    public bool IsListening => State == VoiceState.Listening;
    public bool IsThinking => State == VoiceState.Thinking;
    public bool IsSpeaking => State == VoiceState.Speaking;

    public bool IsHandsFree
    {
        get => _isHandsFree;
        private set
        {
            if (Set(ref _isHandsFree, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    /// <summary>A problem to show the user (empty when everything is fine).</summary>
    public string Message
    {
        get => _message;
        private set => Set(ref _message, value);
    }

    public string StatusText => State switch
    {
        VoiceState.Listening => "Listening...",
        VoiceState.Thinking => "Thinking...",
        VoiceState.Speaking => "Speaking...",
        VoiceState.Idle => "Say the wake word, or click the mic to talk",
        _ => "Voice is off"
    };

    public void Start()
    {
        if (IsHandsFree)
            return;

        IReadOnlyList<string> missing = _missingFiles();
        if (missing.Count > 0)
        {
            Message = $"Voice isn't set up yet: {missing.Count} model file(s) missing. Open Settings for details.";
            return;
        }

        Message = "";
        _cts = new CancellationTokenSource();

        VoiceLoop loop = new(_wake, _listener, _speaker, (text, ct) => _chat.RespondAsync(text, ct));
        loop.StateChanged += state => _ui.Post(() => State = state);
        loop.Error += ex => _ui.Post(() => Message = $"Voice problem: {ex.Message}");

        IsHandsFree = true;
        State = VoiceState.Idle;

        CancellationToken token = _cts.Token;
        _running = Task.Run(async () =>
        {
            try
            {
                await loop.RunAsync(token);
            }
            finally
            {
                _ui.Post(() =>
                {
                    IsHandsFree = false;
                    State = VoiceState.Stopped;
                });
            }
        });
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>Stops and waits for the loop to finish (used on exit and in tests).</summary>
    public async Task StopAsync()
    {
        Stop();

        try { await _running; } catch (OperationCanceledException) { }
    }

    /// <summary>Click-to-talk: starts the loop if needed and listens right now.</summary>
    public void Talk()
    {
        Start();

        if (IsHandsFree)
            _wake.Trigger();
    }
}
