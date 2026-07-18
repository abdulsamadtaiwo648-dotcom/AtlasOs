using System.Diagnostics;
using System.Runtime.Versioning;
using Atlas.System.Interfaces;
using Atlas.System.Models;
using Microsoft.Win32;

namespace Atlas.System.Providers;

/// <summary>
/// Discovers and launches applications on Windows by searching:
/// 1. The Windows Start Menu (user and all-users shortcuts)
/// 2. The Windows Registry (installed programs)
/// 3. Common application paths (AppData, LocalAppData, Desktop)
/// </summary>
[SupportedOSPlatform("windows")]


/// <summary>
/// Discovers and launches applications on Windows by searching:
/// 1. The Windows Start Menu (user and all-users shortcuts)
/// 2. The Windows Registry (installed programs)
/// 3. Common application paths (AppData, LocalAppData, Desktop)
/// </summary>
public class WindowsAppLauncher : IAppLauncher
{
    // Common known app name aliases so the user can say "open chrome" instead of "google chrome"
    private static readonly Dictionary<string, string> AppAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "chrome",       "Google Chrome" },
        { "google chrome","Google Chrome" },
        { "firefox",      "Firefox" },
        { "camera", "Camera"},
        { "edge",         "Microsoft Edge" },
        { "whatsapp",     "WhatsApp" },
        { "opera",        "Opera" },
        { "opera mini",   "Opera" },
        { "operamini",    "Opera" },
        { "notepad",      "Notepad" },
        { "calculator",   "Calculator" },
        { "calc",         "Calculator" },
        { "vlc",          "VLC media player" },
        { "vscode",       "Visual Studio Code" },
        { "vs code",      "Visual Studio Code" },
        { "spotify",      "Spotify" },
        { "discord",      "Discord" },
        { "telegram",     "Telegram" },
        { "word",         "Microsoft Word" },
        { "excel",        "Microsoft Excel" },
        { "powerpoint",   "Microsoft PowerPoint" },
        { "Calender", "Calender"},
        { "paint",        "Paint" },
        { "explorer",     "File Explorer" },
        { "file explorer","File Explorer" },
        { "task manager", "Task Manager" },
    };

    public Task<List<InstalledApp>> GetInstalledAppsAsync()
    {
        var apps = new List<InstalledApp>();

        // 1. Scan Start Menu shortcuts
        apps.AddRange(ScanStartMenu());

        // 2. Scan Windows Registry
        apps.AddRange(ScanRegistry());

        // 3. Scan Desktop
        apps.AddRange(ScanDesktop());

        return Task.FromResult(apps.DistinctBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public async Task<InstalledApp?> FindAppAsync(string name)
    {
        // Resolve alias first
        if (AppAliases.TryGetValue(name.Trim(), out var resolvedName))
            name = resolvedName;

        var allApps = await GetInstalledAppsAsync();

        // Exact match first
        var exact = allApps.FirstOrDefault(a =>
            a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // Fuzzy contains match
        return allApps.FirstOrDefault(a =>
            a.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
            name.Contains(a.Name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> LaunchAppAsync(string name)
    {
        // Handle built-in Windows apps that don't need searching
        string lname = name.ToLowerInvariant().Trim();

        if (lname is "settings" or "windows settings" or "setting")
        { Process.Start(new ProcessStartInfo("ms-settings:") { UseShellExecute = true }); return true; }
        if (lname is "notepad")
        { Process.Start("notepad.exe"); return true; }
        if (lname is "calculator" or "calc")
        { Process.Start("calc.exe"); return true; }
        if (lname is "calender" or "Calender")
        { Process.Start("cale.exe");  return true; }
        if (lname is "paint")
        { Process.Start("mspaint.exe"); return true; }
        if (lname is "task manager" or "taskmanager")
        { Process.Start("taskmgr.exe"); return true; }
        if (lname is "file explorer" or "explorer" or "files")
        { Process.Start("explorer.exe"); return true; }
        if (lname is "cmd" or "command prompt" or "terminal")
        { Process.Start("cmd.exe"); return true; }
        if (lname is "control panel" or "control")
        { Process.Start(new ProcessStartInfo("control.exe") { UseShellExecute = true }); return true; }
        if (lname is "snipping tool" or "snip")
        { Process.Start(new ProcessStartInfo("snippingtool.exe") { UseShellExecute = true }); return true; }

        var app = await FindAppAsync(name);
        if (app == null) return false;

        var psi = new ProcessStartInfo
        {
            FileName = app.ExecutablePath,
            UseShellExecute = true
        };
        Process.Start(psi);
        return true;
    }

    public Task<bool> CloseAppAsync(string name)
    {
        // Resolve alias
        if (AppAliases.TryGetValue(name.Trim(), out var resolvedName))
            name = resolvedName;

        var processes = Process.GetProcesses()
            .Where(p => p.ProcessName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                        p.MainWindowTitle.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var p in processes)
        {
            try { p.Kill(); }
            catch { /* ignore access denied */ }
        }

        return Task.FromResult(processes.Any());
    }

    public Task<List<string>> GetRunningAppsAsync()
    {
        var running = Process.GetProcesses()
            .Where(p => !string.IsNullOrWhiteSpace(p.MainWindowTitle))
            .Select(p => p.MainWindowTitle)
            .Distinct()
            .ToList();

        return Task.FromResult(running);
    }

    // ==========================================
    // Private Helpers
    // ==========================================

    private static IEnumerable<InstalledApp> ScanStartMenu()
    {
        var apps = new List<InstalledApp>();
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        };

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;

            foreach (var lnk in Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    // Use WScript.Shell COM object to resolve .lnk shortcut targets
                    dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
                    dynamic shortcut = shell.CreateShortcut(lnk);
                    string targetPath = shortcut.TargetPath;

                    if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
                    {
                        apps.Add(new InstalledApp
                        {
                            Name = Path.GetFileNameWithoutExtension(lnk),
                            ExecutablePath = targetPath,
                            Source = "StartMenu"
                        });
                    }
                }
                catch { /* skip inaccessible shortcuts */ }
            }
        }

        return apps;
    }

    private static IEnumerable<InstalledApp> ScanRegistry()
    {
        var apps = new List<InstalledApp>();
        var keys = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var keyPath in keys)
        {
            using var root = Registry.LocalMachine.OpenSubKey(keyPath);
            if (root == null) continue;

            foreach (var sub in root.GetSubKeyNames())
            {
                using var key = root.OpenSubKey(sub);
                if (key == null) continue;

                var name = key.GetValue("DisplayName") as string;
                var path = key.GetValue("InstallLocation") as string;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path)) continue;

                // Try to find an exe inside the install location
                if (Directory.Exists(path))
                {
                    var exes = Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly)
                        .OrderBy(f => f.Length)
                        .ToList();

                    if (exes.Any())
                    {
                        apps.Add(new InstalledApp
                        {
                            Name = name,
                            ExecutablePath = exes.First(),
                            Source = "Registry"
                        });
                    }
                }
            }
        }

        return apps;
    }

    private static IEnumerable<InstalledApp> ScanDesktop()
    {
        var apps = new List<InstalledApp>();
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
        };

        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder)) continue;

            // Resolve .lnk files
            try
            {
                foreach (var lnk in Directory.GetFiles(folder, "*.lnk", SearchOption.AllDirectories))
                {
                    try
                    {
                        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
                        dynamic shortcut = shell.CreateShortcut(lnk);
                        string targetPath = shortcut.TargetPath;

                        if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
                        {
                            apps.Add(new InstalledApp
                            {
                                Name = Path.GetFileNameWithoutExtension(lnk),
                                ExecutablePath = targetPath,
                                Source = "Desktop"
                            });
                        }
                    }
                    catch { /* skip inaccessible shortcuts */ }
                }
            }
            catch { }

            // Resolve .exe files
            try
            {
                foreach (var exe in Directory.GetFiles(folder, "*.exe", SearchOption.AllDirectories))
                {
                    try
                    {
                        apps.Add(new InstalledApp
                        {
                            Name = Path.GetFileNameWithoutExtension(exe),
                            ExecutablePath = exe,
                            Source = "Desktop"
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        return apps;
    }
}
