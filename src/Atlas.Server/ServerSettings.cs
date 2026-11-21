using System.Security.Cryptography;
using System.Text.Json;

namespace Atlas.Server;

/// <summary>Options for the phone server. Stored in server.json next to the other Atlas files.</summary>
public class ServerSettings
{
    public int Port { get; set; } = 5050;

    /// <summary>The password the phone must send. Generated on first run.</summary>
    public string Token { get; set; } = "";

    /// <summary>"0.0.0.0" lets other devices connect; "127.0.0.1" keeps it on this PC only.</summary>
    public string Bind { get; set; } = "0.0.0.0";

    /// <summary>
    /// By default only private-network addresses (home Wi-Fi, Tailscale, loopback) may connect.
    /// Turn this on only if you really want Atlas reachable from the public internet.
    /// </summary>
    public bool AllowPublicInternet { get; set; } = false;
}

public sealed class ServerSettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public ServerSettingsStore(string? path = null)
    {
        FilePath = path ?? DefaultPath();
    }

    public string FilePath { get; }

    public static string DefaultPath()
    {
        string? env = Environment.GetEnvironmentVariable("ATLAS_SERVER_SETTINGS");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtlasOS",
            "server.json");
    }

    /// <summary>Loads the settings, creating the file (with a fresh random password) the first time.</summary>
    public ServerSettings LoadOrCreate()
    {
        ServerSettings settings = new();

        try
        {
            if (File.Exists(FilePath))
                settings = JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(FilePath)) ?? new ServerSettings();
        }
        catch (Exception)
        {
            // Unreadable file: start over with defaults (and a new password).
        }

        bool changed = false;

        if (settings.Port is < 1 or > 65535)
        {
            settings.Port = 5050;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.Token) || settings.Token.Length < 24)
        {
            settings.Token = GenerateToken();
            changed = true;
        }

        if (changed || !File.Exists(FilePath))
            Save(settings);

        return settings;
    }

    public ServerSettings RotateToken()
    {
        ServerSettings settings = LoadOrCreate();
        settings.Token = GenerateToken();
        Save(settings);
        return settings;
    }

    public void Save(ServerSettings settings)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(FilePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));

        // Keep the password file private to this user where the OS supports it.
        try
        {
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(FilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception)
        {
            // best effort
        }
    }

    /// <summary>32 random bytes, URL-safe, so it can sit in a link fragment.</summary>
    public static string GenerateToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
