using System.Text.Json;

namespace Atlas.Core.Settings;

public interface ISettingsService
{
    AtlasSettings Current { get; }

    /// <summary>Raised after settings were changed and saved.</summary>
    event Action? Changed;

    /// <summary>Applies a change to a copy, validates it, saves it and publishes it.</summary>
    void Update(Action<AtlasSettings> change);

    string FilePath { get; }
}

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly object _lock = new();
    private AtlasSettings _current;

    public JsonSettingsService(string? path = null)
    {
        FilePath = path ?? DefaultPath();
        _current = Load();
    }

    public string FilePath { get; }

    public AtlasSettings Current
    {
        get { lock (_lock) return _current; }
    }

    public event Action? Changed;

    public void Update(Action<AtlasSettings> change)
    {
        lock (_lock)
        {
            AtlasSettings next = _current.Clone();
            change(next);
            _current = Sanitize(next);
            Save(_current);
        }

        Changed?.Invoke();
    }

    public static string DefaultPath()
    {
        string? env = Environment.GetEnvironmentVariable("ATLAS_SETTINGS");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtlasOS",
            "settings.json");
    }

    /// <summary>Keeps values usable even if the file was edited by hand.</summary>
    internal static AtlasSettings Sanitize(AtlasSettings settings)
    {
        settings.OllamaUrl = string.IsNullOrWhiteSpace(settings.OllamaUrl)
            ? new AtlasSettings().OllamaUrl
            : settings.OllamaUrl.Trim().TrimEnd('/');

        settings.Model = string.IsNullOrWhiteSpace(settings.Model)
            ? new AtlasSettings().Model
            : settings.Model.Trim();

        settings.SpeechSpeed = double.IsNaN(settings.SpeechSpeed)
            ? 1.0
            : Math.Clamp(settings.SpeechSpeed, 0.5, 2.0);

        return settings;
    }

    private AtlasSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                AtlasSettings? loaded = JsonSerializer.Deserialize<AtlasSettings>(File.ReadAllText(FilePath));
                if (loaded != null)
                    return Sanitize(loaded);
            }
        }
        catch (Exception)
        {
            // A broken settings file must never stop Atlas from starting.
        }

        return new AtlasSettings();
    }

    private void Save(AtlasSettings settings)
    {
        try
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(FilePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception)
        {
            // Not being able to save shouldn't crash the app; the change still applies this session.
        }
    }
}
