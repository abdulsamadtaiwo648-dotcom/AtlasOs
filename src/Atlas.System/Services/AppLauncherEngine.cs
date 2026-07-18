using Atlas.System.Interfaces;

namespace Atlas.System.Services;

/// <summary>
/// The brain of the Atlas System module.
/// Parses natural language OS commands and dispatches them to IAppLauncher.
/// </summary>
public class AppLauncherEngine
{
    private readonly IAppLauncher _launcher;

    public AppLauncherEngine(IAppLauncher launcher)
    {
        _launcher = launcher;
    }

    public async Task<string> ProcessCommandAsync(string input)
    {
        string text = input.Trim().ToLowerInvariant();

        // ── List running apps ──────────────────────────────
        if (text.Contains("what") && (text.Contains("open") || text.Contains("running")))
        {
            var running = await _launcher.GetRunningAppsAsync();
            if (!running.Any())
                return "No applications are currently open.";

            return "Currently open:\n" + string.Join("\n", running.Select(r => $"  • {r}"));
        }

        // ── Close / kill ───────────────────────────────────
        if (text.StartsWith("close") || text.StartsWith("kill") || text.StartsWith("exit"))
        {
            string appName = ExtractAppName(text, "close", "kill", "exit");
            if (string.IsNullOrWhiteSpace(appName))
                return "Which application would you like me to close?";

            bool closed = await _launcher.CloseAppAsync(appName);
            return closed
                ? $"{Capitalize(appName)} has been closed."
                : $"I could not find a running application called \"{appName}\".";
        }

        // ── Open / launch / start ──────────────────────────
        if (text.StartsWith("open") || text.StartsWith("launch") || text.StartsWith("start") ||
            text.Contains("open ") || text.Contains("launch ") || text.Contains("run "))
        {
            string appName = ExtractAppName(text, "open", "launch", "start", "run");
            if (string.IsNullOrWhiteSpace(appName))
                return "Which application would you like me to open?";

            bool launched = await _launcher.LaunchAppAsync(appName);
            return launched
                ? $"Opening {Capitalize(appName)} now."
                : $"I could not find \"{appName}\" installed on this device. Please make sure it is installed.";
        }

        return "I did not understand that system command.";
    }

    private static string ExtractAppName(string text, params string[] verbs)
    {
        // Strip conversational prefixes before searching for verb
        string[] prefixes = {
            "can you ", "could you ", "please ", "atlas ", "would you ",
            "hey atlas ", "i want to ", "i need to ", "i want you to "
        };
        foreach (var prefix in prefixes)
            if (text.StartsWith(prefix)) text = text[prefix.Length..].Trim();

        foreach (var verb in verbs)
        {
            int idx = text.IndexOf(verb, StringComparison.Ordinal);
            if (idx >= 0)
            {
                string after = text[(idx + verb.Length)..].Trim();

                // Strip common filler words
                foreach (var filler in new[] { "the ", "my ", "a ", "an ", "app ", "application " })
                    if (after.StartsWith(filler)) after = after[filler.Length..].Trim();

                if (!string.IsNullOrWhiteSpace(after)) return after;
            }
        }

        return "";
    }

    private static string Capitalize(string s)
        => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
