using Atlas.Hosting;
using Atlas.Server;

// Usage:
//   Atlas.Server                 start the phone server
//   Atlas.Server --port 6000     use a different port (saved)
//   Atlas.Server --new-password  make a new password (the old one stops working)
ServerSettingsStore store = new();

ServerSettings settings = args.Contains("--new-password") ? store.RotateToken() : store.LoadOrCreate();

int portIndex = Array.IndexOf(args, "--port");
if (portIndex >= 0 && portIndex + 1 < args.Length && int.TryParse(args[portIndex + 1], out int port) && port is > 0 and < 65536)
{
    settings.Port = port;
    store.Save(settings);
}

var app = AtlasServerApp.Build(args, settings, builder =>
    builder.WebHost.UseUrls($"http://{settings.Bind}:{settings.Port}"));

await app.Services.InitializeAtlasAsync();

Console.WriteLine();
Console.WriteLine("==========================================");
Console.WriteLine("        Atlas phone server");
Console.WriteLine("==========================================");
Console.WriteLine();

IReadOnlyList<string> addresses = AtlasServerApp.LocalAddresses();

if (addresses.Count == 0)
{
    Console.WriteLine("No network connection found. Connect this PC to your Wi-Fi, then restart.");
}
else
{
    Console.WriteLine("On your phone (same Wi-Fi), open one of these. The link includes your password,");
    Console.WriteLine("so only share it with yourself:");
    Console.WriteLine();
    foreach (string address in addresses)
        Console.WriteLine($"   http://{address}:{settings.Port}/#t={settings.Token}");
}

Console.WriteLine();
Console.WriteLine($"Password file: {store.FilePath}");
Console.WriteLine("New password:  dotnet run --project src/Atlas.Server -- --new-password");
Console.WriteLine(settings.AllowPublicInternet
    ? "WARNING: reachable from the public internet (AllowPublicInternet is on)."
    : "Only devices on your private network (Wi-Fi, Tailscale) can connect.");
Console.WriteLine();
Console.WriteLine("Press Ctrl+C to stop.");

await app.RunAsync();

public partial class Program { }
