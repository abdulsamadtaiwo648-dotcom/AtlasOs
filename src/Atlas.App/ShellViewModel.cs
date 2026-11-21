using Atlas.App.Chat;
using Atlas.App.Memory;
using Atlas.App.Mvvm;
using Atlas.App.Settings;
using Atlas.App.Voice;
using Atlas.Core.Settings;

namespace Atlas.App;

public enum AppPage
{
    Chat,
    Memory,
    Settings
}

/// <summary>The main window: which page is showing, plus the pages themselves.</summary>
public sealed class ShellViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private AppPage _page = AppPage.Chat;

    public ShellViewModel(
        ChatViewModel chat,
        MemoryViewModel memory,
        SettingsViewModel settings,
        VoiceViewModel voice,
        ISettingsService settingsService)
    {
        Chat = chat;
        Memory = memory;
        Settings = settings;
        Voice = voice;
        _settings = settingsService;

        ShowChatCommand = new RelayCommand(() => Page = AppPage.Chat);
        ShowMemoryCommand = new RelayCommand(() => Page = AppPage.Memory);
        ShowSettingsCommand = new RelayCommand(() => Page = AppPage.Settings);
    }

    public ChatViewModel Chat { get; }
    public MemoryViewModel Memory { get; }
    public SettingsViewModel Settings { get; }
    public VoiceViewModel Voice { get; }

    public RelayCommand ShowChatCommand { get; }
    public RelayCommand ShowMemoryCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }

    public AppPage Page
    {
        get => _page;
        set
        {
            if (!Set(ref _page, value))
                return;

            OnPropertyChanged(nameof(IsChatPage));
            OnPropertyChanged(nameof(IsMemoryPage));
            OnPropertyChanged(nameof(IsSettingsPage));

            // Show fresh data when a page is opened (things may have changed by voice or chat).
            if (value == AppPage.Memory)
                Memory.Refresh();
            else if (value == AppPage.Settings)
                Settings.RefreshSpeechStatus();
        }
    }

    public bool IsChatPage => Page == AppPage.Chat;
    public bool IsMemoryPage => Page == AppPage.Memory;
    public bool IsSettingsPage => Page == AppPage.Settings;

    /// <summary>Called once the window is shown.</summary>
    public void OnStarted()
    {
        if (_settings.Current.StartHandsFree)
            Voice.Start();
    }

    public Task OnClosingAsync() => Voice.StopAsync();
}
