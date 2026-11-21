namespace Atlas.Memory;

public static class MemoryPaths
{
    /// <summary>
    /// ATLAS_DB if set, otherwise atlas.db in the user's local app-data folder.
    /// </summary>
    public static string DefaultDatabasePath()
    {
        string? env = Environment.GetEnvironmentVariable("ATLAS_DB");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtlasOS",
            "atlas.db");
    }
}
