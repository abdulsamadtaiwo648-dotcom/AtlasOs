using Atlas.System.Models;

namespace Atlas.System.Interfaces;

/// <summary>
/// The primary interface for all OS-level app launching operations.
/// Atlas only communicates through this interface — never directly with the OS.
/// </summary>
public interface IAppLauncher
{
    /// <summary>
    /// Discovers all installed applications on the host machine.
    /// </summary>
    Task<List<InstalledApp>> GetInstalledAppsAsync();

    /// <summary>
    /// Finds a specific app by its friendly name (fuzzy-matched).
    /// </summary>
    Task<InstalledApp?> FindAppAsync(string name);

    /// <summary>
    /// Launches an application by its name.
    /// </summary>
    Task<bool> LaunchAppAsync(string name);

    /// <summary>
    /// Closes/kills a running application by name.
    /// </summary>
    Task<bool> CloseAppAsync(string name);

    /// <summary>
    /// Returns a list of currently running application process names.
    /// </summary>
    Task<List<string>> GetRunningAppsAsync();
}
