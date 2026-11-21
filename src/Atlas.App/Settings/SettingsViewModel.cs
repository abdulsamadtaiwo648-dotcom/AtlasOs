using System.Collections.ObjectModel;
using Atlas.App.Mvvm;
using Atlas.Core.Settings;
using Atlas.Memory;
using Atlas.Speech;

namespace Atlas.App.Settings;

/// <summary>Finds out which models an Ollama server has installed.</summary>
public interface IOllamaClient
{
    Task<IReadOnlyList<string>> ListModelsAsync(string baseUrl, CancellationToken cancellationToken);
}

/// <summary>Settings screen. Every change is saved straight away and used by the next request.</summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IOllamaClient _ollama;

    private string _ollamaUrl;
    private string _model;
    private bool _speakReplies;
    private double _speechSpeed;
    private bool _startHandsFree;
    private string _connectionStatus = "Not checked yet.";
    private bool _isChecking;

    public SettingsViewModel(ISettingsService settings, IOllamaClient ollama)
    {
        _settings = settings;
        _ollama = ollama;

        AtlasSettings current = settings.Current;
        _ollamaUrl = current.OllamaUrl;
        _model = current.Model;
        _speakReplies = current.SpeakReplies;
        _speechSpeed = current.SpeechSpeed;
        _startHandsFree = current.StartHandsFree;

        CheckConnectionCommand = new AsyncRelayCommand(CheckConnectionAsync, () => !_isChecking);
        RefreshSpeechStatus();
    }

    public ObservableCollection<string> AvailableModels { get; } = new();

    public AsyncRelayCommand CheckConnectionCommand { get; }

    public string OllamaUrl
    {
        get => _ollamaUrl;
        set
        {
            if (Set(ref _ollamaUrl, value))
                Save(s => s.OllamaUrl = value);
        }
    }

    public string Model
    {
        get => _model;
        set
        {
            if (Set(ref _model, value))
                Save(s => s.Model = value);
        }
    }

    public bool SpeakReplies
    {
        get => _speakReplies;
        set
        {
            if (Set(ref _speakReplies, value))
                Save(s => s.SpeakReplies = value);
        }
    }

    public double SpeechSpeed
    {
        get => _speechSpeed;
        set
        {
            if (Set(ref _speechSpeed, value))
            {
                Save(s => s.SpeechSpeed = value);
                OnPropertyChanged(nameof(SpeechSpeedText));
            }
        }
    }

    public string SpeechSpeedText => $"{SpeechSpeed:0.0}x";

    public bool StartHandsFree
    {
        get => _startHandsFree;
        set
        {
            if (Set(ref _startHandsFree, value))
                Save(s => s.StartHandsFree = value);
        }
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => Set(ref _connectionStatus, value);
    }

    // ── Read-only information ──────────────────────────────────────────────

    public string SettingsFile => _settings.FilePath;
    public string MemoryDatabase => MemoryPaths.DefaultDatabasePath();
    public string SpeechFolder => SpeechPaths.Root;

    public string SpeechStatus { get; private set; } = "";
    public bool SpeechReady { get; private set; }

    public void RefreshSpeechStatus()
    {
        IReadOnlyList<string> missing = SpeechPaths.MissingModelFiles();
        SpeechReady = missing.Count == 0;
        SpeechStatus = SpeechReady
            ? "Voice models found."
            : $"{missing.Count} voice model file(s) missing. Put them under {SpeechPaths.ModelsDir} " +
              "or set the ATLAS_HOME environment variable.";

        OnPropertyChanged(nameof(SpeechStatus));
        OnPropertyChanged(nameof(SpeechReady));
    }

    // ── Checking Ollama ────────────────────────────────────────────────────

    public async Task CheckConnectionAsync()
    {
        _isChecking = true;
        CheckConnectionCommand.RaiseCanExecuteChanged();
        ConnectionStatus = "Checking...";

        try
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
            IReadOnlyList<string> models = await _ollama.ListModelsAsync(OllamaUrl, timeout.Token);

            AvailableModels.Clear();
            foreach (string name in models)
                AvailableModels.Add(name);

            ConnectionStatus = models.Count == 0
                ? "Connected, but no models are installed. Run: ollama pull gemma3:4b"
                : models.Contains(Model)
                    ? $"Connected. {models.Count} model(s) installed."
                    : $"Connected, but \"{Model}\" isn't installed. Pick one from the list or run: ollama pull {Model}";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            ConnectionStatus = "Couldn't reach Ollama there. Is it running?";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Check failed: {ex.Message}";
        }
        finally
        {
            _isChecking = false;
            CheckConnectionCommand.RaiseCanExecuteChanged();
        }
    }

    private void Save(Action<AtlasSettings> change) => _settings.Update(change);
}
