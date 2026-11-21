using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Atlas.Core.Interfaces;
using Atlas.Core.Settings;
using Atlas.Memory;
using Atlas.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Tests;

public class NetworkPolicyTests
{
    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("192.168.1.50", true)]
    [InlineData("10.4.5.6", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.1", true)]
    [InlineData("100.64.0.9", true)]       // Tailscale
    [InlineData("100.127.1.1", true)]
    [InlineData("169.254.10.10", true)]
    [InlineData("fd12:3456::1", true)]     // IPv6 unique-local
    [InlineData("fe80::1", true)]
    [InlineData("::ffff:192.168.0.7", true)] // IPv4 written as IPv6
    [InlineData("8.8.8.8", false)]
    [InlineData("172.32.0.1", false)]      // just outside 172.16/12
    [InlineData("172.15.0.1", false)]
    [InlineData("100.128.0.1", false)]     // just outside Tailscale range
    [InlineData("192.169.0.1", false)]
    [InlineData("2606:4700::1111", false)]
    public void OnlyPrivateAddressesAreAllowed(string ip, bool expected)
    {
        Assert.Equal(expected, NetworkPolicy.IsAllowed(IPAddress.Parse(ip), allowPublicInternet: false));
    }

    [Fact]
    public void UnknownAddressIsRefused_UnlessPublicAccessIsOn()
    {
        Assert.False(NetworkPolicy.IsAllowed(null, false));
        Assert.True(NetworkPolicy.IsAllowed(IPAddress.Parse("8.8.8.8"), true));
    }
}

public class SecurityHelpersTests
{
    [Fact]
    public void TokenCheck_OnlyAcceptsTheExactToken()
    {
        Assert.True(TokenCheck.Matches("abc123", "abc123"));
        Assert.False(TokenCheck.Matches("abc124", "abc123"));
        Assert.False(TokenCheck.Matches("abc123 ", "abc123"));
        Assert.False(TokenCheck.Matches("", "abc123"));
        Assert.False(TokenCheck.Matches(null, "abc123"));
        Assert.False(TokenCheck.Matches("anything", ""));
    }

    [Fact]
    public void Throttle_LocksAfterTooManyFailures_ThenReleases()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
        AuthThrottle throttle = new(maxFailures: 3, window: TimeSpan.FromMinutes(10),
            lockout: TimeSpan.FromMinutes(5), now: () => now);

        throttle.RecordFailure("phone");
        throttle.RecordFailure("phone");
        Assert.False(throttle.IsLockedOut("phone"));

        throttle.RecordFailure("phone");
        Assert.True(throttle.IsLockedOut("phone"));
        Assert.False(throttle.IsLockedOut("someone-else"));

        now += TimeSpan.FromMinutes(4);
        Assert.True(throttle.IsLockedOut("phone"));

        now += TimeSpan.FromMinutes(2);
        Assert.False(throttle.IsLockedOut("phone"));
    }

    [Fact]
    public void Throttle_OldFailuresAndSuccessesDoNotCount()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-01-01T12:00:00Z");
        AuthThrottle throttle = new(maxFailures: 3, window: TimeSpan.FromMinutes(10), now: () => now);

        throttle.RecordFailure("a");
        throttle.RecordFailure("a");
        now += TimeSpan.FromMinutes(11);   // those two are too old to matter
        throttle.RecordFailure("a");
        Assert.False(throttle.IsLockedOut("a"));

        throttle.RecordFailure("a");
        throttle.RecordSuccess("a");       // a correct password clears the count
        throttle.RecordFailure("a");
        throttle.RecordFailure("a");
        Assert.False(throttle.IsLockedOut("a"));
    }
}

public class ServerSettingsStoreTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), $"atlas_server_{Guid.NewGuid():N}.json");

    [Fact]
    public void FirstRun_CreatesAStrongPassword_AndKeepsItAfterwards()
    {
        string path = TempPath();
        try
        {
            ServerSettings first = new ServerSettingsStore(path).LoadOrCreate();
            ServerSettings second = new ServerSettingsStore(path).LoadOrCreate();

            Assert.True(first.Token.Length >= 40);
            Assert.Equal(first.Token, second.Token);
            Assert.Equal(5050, first.Port);
            Assert.False(first.AllowPublicInternet);
            Assert.DoesNotContain('+', first.Token);
            Assert.DoesNotContain('/', first.Token);
            Assert.DoesNotContain('=', first.Token);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void NewPassword_ReplacesTheOldOne()
    {
        string path = TempPath();
        try
        {
            ServerSettingsStore store = new(path);
            string old = store.LoadOrCreate().Token;

            string fresh = store.RotateToken().Token;

            Assert.NotEqual(old, fresh);
            Assert.Equal(fresh, store.LoadOrCreate().Token);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DamagedOrWeakFiles_AreRepaired()
    {
        string path = TempPath();
        try
        {
            File.WriteAllText(path, "{ not json");
            Assert.True(new ServerSettingsStore(path).LoadOrCreate().Token.Length >= 40);

            File.WriteAllText(path, "{\"Token\":\"short\",\"Port\":99999}");
            ServerSettings repaired = new ServerSettingsStore(path).LoadOrCreate();

            Assert.True(repaired.Token.Length >= 40);
            Assert.Equal(5050, repaired.Port);
        }
        finally { File.Delete(path); }
    }
}

/// <summary>Runs the real server in memory (no network), with the AI and storage replaced by fakes.</summary>
internal sealed class ServerRig : IAsyncDisposable
{
    public const string Token = "test-token-0123456789-abcdefghijklmnopqrstuvwxyz";

    private readonly TempDb _db = new();
    private readonly TempSettings _settingsFile = new();
    public readonly ScriptedAi Ai = new();
    public readonly MemoryStore Store = new();
    public readonly WebApplication App;
    public readonly HttpClient Client;

    private ServerRig(WebApplication app, HttpClient client, ServerRigParts parts)
    {
        App = app;
        Client = client;
        _db = parts.Db;
        _settingsFile = parts.Settings;
        Ai = parts.Ai;
        Store = parts.Store;
    }

    private sealed record ServerRigParts(TempDb Db, TempSettings Settings, ScriptedAi Ai, MemoryStore Store);

    public static async Task<ServerRig> StartAsync(string remoteIp = "192.168.1.50", bool allowPublic = false)
    {
        ServerRigParts parts = new(new TempDb(), new TempSettings(), new ScriptedAi(), new MemoryStore());
        ServerSettings settings = new() { Token = Token, AllowPublicInternet = allowPublic };

        WebApplication app = AtlasServerApp.Build(
            Array.Empty<string>(),
            settings,
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Services.AddSingleton<IAIProvider>(parts.Ai);
                builder.Services.AddSingleton<IConversationStore>(parts.Store);
                builder.Services.AddSingleton<ISettingsService>(parts.Settings.Open());
                builder.Services.AddSingleton<IMemoryService>(parts.Db.Open());
            },
            pipeline => pipeline.Use(async (context, next) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
                await next();
            }));

        await app.StartAsync();
        HttpClient client = app.GetTestClient();
        return new ServerRig(app, client, parts);
    }

    public HttpRequestMessage Request(HttpMethod method, string url, object? json = null, bool authorized = true)
    {
        HttpRequestMessage request = new(method, url);
        if (authorized)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        if (json != null)
            request.Content = JsonContent.Create(json);
        return request;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? json = null, bool authorized = true)
    {
        using HttpRequestMessage request = Request(method, url, json, authorized);
        return await Client.SendAsync(request);
    }

    public async Task<JsonElement> JsonAsync(HttpMethod method, string url, object? json = null)
    {
        HttpResponseMessage response = await SendAsync(method, url, json);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    public async Task<string> ChatAsync(string text)
    {
        HttpResponseMessage response = await SendAsync(HttpMethod.Post, "/api/chat", new { text });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.StopAsync();
        await App.DisposeAsync();
        _db.Dispose();
        _settingsFile.Dispose();
    }
}

public class ServerApiTests
{
    [Fact]
    public async Task Api_RequiresThePassword()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await rig.SendAsync(HttpMethod.Get, "/api/status", authorized: false)).StatusCode);

        using HttpRequestMessage wrong = rig.Request(HttpMethod.Get, "/api/status", authorized: false);
        wrong.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "wrong");
        Assert.Equal(HttpStatusCode.Unauthorized, (await rig.Client.SendAsync(wrong)).StatusCode);

        JsonElement status = await rig.JsonAsync(HttpMethod.Get, "/api/status");
        Assert.Equal("Atlas", status.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Password_CanAlsoComeInAnXAtlasTokenHeader()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/status");
        request.Headers.Add("X-Atlas-Token", ServerRig.Token);

        Assert.Equal(HttpStatusCode.OK, (await rig.Client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task PasswordInTheUrl_IsNotAccepted()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        HttpResponseMessage response = await rig.SendAsync(HttpMethod.Get, "/api/status?token=" + ServerRig.Token, authorized: false);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicInternetClients_AreRefused_EvenWithTheRightPassword()
    {
        await using ServerRig rig = await ServerRig.StartAsync(remoteIp: "8.8.8.8");

        Assert.Equal(HttpStatusCode.Forbidden, (await rig.SendAsync(HttpMethod.Get, "/api/status")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await rig.SendAsync(HttpMethod.Get, "/", authorized: false)).StatusCode);
    }

    [Fact]
    public async Task PublicInternetClients_AreAllowedOnlyWhenSwitchedOn()
    {
        await using ServerRig rig = await ServerRig.StartAsync(remoteIp: "8.8.8.8", allowPublic: true);

        Assert.Equal(HttpStatusCode.OK, (await rig.SendAsync(HttpMethod.Get, "/api/status")).StatusCode);
    }

    [Fact]
    public async Task TooManyWrongPasswords_LockTheClientOut()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        for (int i = 0; i < 8; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await rig.SendAsync(HttpMethod.Get, "/api/status", authorized: false)).StatusCode);

        // Now even the correct password is refused for a while.
        Assert.Equal(HttpStatusCode.TooManyRequests, (await rig.SendAsync(HttpMethod.Get, "/api/status")).StatusCode);
    }

    [Fact]
    public async Task PageIsPublic_ButCarriesStrictSecurityHeaders()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        HttpResponseMessage page = await rig.SendAsync(HttpMethod.Get, "/", authorized: false);

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("Atlas", await page.Content.ReadAsStringAsync());
        Assert.Contains("script-src 'self'", page.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("nosniff", page.Headers.GetValues("X-Content-Type-Options").Single());

        HttpResponseMessage api = await rig.SendAsync(HttpMethod.Get, "/api/status");
        Assert.Equal("no-store", api.Headers.GetValues("Cache-Control").Single());

        // none of the page assets contain the password or inline script
        string html = await page.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ServerRig.Token, html);
        Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public async Task Chat_StreamsTheReply_AndSavesTheConversation()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        string reply = await rig.ChatAsync("hello atlas");

        Assert.Equal("Hello there. How can I help you today?", reply);

        JsonElement chat = await rig.JsonAsync(HttpMethod.Get, "/api/chat");
        JsonElement[] messages = chat.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal("hello atlas", messages[0].GetProperty("text").GetString());
        Assert.Equal("assistant", messages[1].GetProperty("role").GetString());
        Assert.Equal("hello atlas", chat.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AnswersAtlasGivesItself_AreAlsoSavedInTheChat()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        Assert.Equal("4", await rig.ChatAsync("calculate 2+2"));
        Assert.StartsWith("Got it", await rig.ChatAsync("remember that I like tea"));
        await rig.ChatAsync("/help"); // slash commands are not part of the chat

        JsonElement chat = await rig.JsonAsync(HttpMethod.Get, "/api/chat");
        string[] texts = chat.GetProperty("messages").EnumerateArray().Select(m => m.GetProperty("text").GetString()!).ToArray();

        Assert.Equal(4, texts.Length);
        Assert.Equal("calculate 2+2", texts[0]);
        Assert.Equal("4", texts[1]);
    }

    [Fact]
    public async Task Chat_RejectsEmptyAndOverlongMessages()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await rig.SendAsync(HttpMethod.Post, "/api/chat", new { text = "   " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await rig.SendAsync(HttpMethod.Post, "/api/chat", new { })).StatusCode);

        HttpResponseMessage tooLong = await rig.SendAsync(
            HttpMethod.Post, "/api/chat", new { text = new string('a', AtlasServerApp.MaxMessageLength + 1) });
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task Chat_SaysBusyIfAnotherRequestIsRunning()
    {
        await using ServerRig rig = await ServerRig.StartAsync();
        SemaphoreSlim gate = rig.App.Services.GetRequiredService<SemaphoreSlim>();

        await gate.WaitAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Conflict, (await rig.SendAsync(HttpMethod.Post, "/api/chat", new { text = "hi" })).StatusCode);
        }
        finally { gate.Release(); }

        Assert.Equal(HttpStatusCode.OK, (await rig.SendAsync(HttpMethod.Post, "/api/chat", new { text = "hi" })).StatusCode);
    }

    [Fact]
    public async Task ChatHistory_NewOpenAndDelete()
    {
        await using ServerRig rig = await ServerRig.StartAsync();
        await rig.ChatAsync("first topic");
        Guid firstId = Guid.Parse((await rig.JsonAsync(HttpMethod.Get, "/api/chat")).GetProperty("id").GetString()!);

        await rig.JsonAsync(HttpMethod.Post, "/api/chat/new");
        Assert.Empty((await rig.JsonAsync(HttpMethod.Get, "/api/chat")).GetProperty("messages").EnumerateArray());

        await rig.ChatAsync("second topic");

        JsonElement[] chats = (await rig.JsonAsync(HttpMethod.Get, "/api/chats")).EnumerateArray().ToArray();
        Assert.Equal(2, chats.Length);
        Assert.Equal("second topic", chats[0].GetProperty("title").GetString());
        Assert.True(chats[0].GetProperty("current").GetBoolean());

        JsonElement reopened = await rig.JsonAsync(HttpMethod.Post, $"/api/chats/{firstId}/open");
        Assert.Equal("first topic", reopened.GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await rig.SendAsync(HttpMethod.Delete, $"/api/chats/{firstId}")).StatusCode);
        Assert.Single((await rig.JsonAsync(HttpMethod.Get, "/api/chats")).EnumerateArray());

        Assert.Equal(HttpStatusCode.NotFound, (await rig.SendAsync(HttpMethod.Post, $"/api/chats/{Guid.NewGuid()}/open")).StatusCode);
    }

    [Fact]
    public async Task Memory_AddSearchPinAndForget()
    {
        await using ServerRig rig = await ServerRig.StartAsync();

        HttpResponseMessage created = await rig.SendAsync(HttpMethod.Post, "/api/memory", new { text = "My sister is called Ola" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        HttpResponseMessage again = await rig.SendAsync(HttpMethod.Post, "/api/memory", new { text = "my sister is called ola" });
        Assert.True(JsonDocument.Parse(await again.Content.ReadAsStringAsync()).RootElement.GetProperty("alreadyKnown").GetBoolean());

        await rig.SendAsync(HttpMethod.Post, "/api/memory", new { text = "I like jollof rice" });

        Assert.Equal(2, (await rig.JsonAsync(HttpMethod.Get, "/api/memory")).GetArrayLength());

        JsonElement found = await rig.JsonAsync(HttpMethod.Get, "/api/memory?q=sister");
        Assert.Equal(1, found.GetArrayLength());
        long id = found[0].GetProperty("id").GetInt64();

        JsonElement pinned = await rig.JsonAsync(HttpMethod.Post, $"/api/memory/{id}/pin", new { pinned = true });
        Assert.True(pinned.GetProperty("pinned").GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await rig.SendAsync(HttpMethod.Delete, $"/api/memory/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await rig.SendAsync(HttpMethod.Delete, $"/api/memory/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await rig.SendAsync(HttpMethod.Post, "/api/memory/9999/pin", new { pinned = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await rig.SendAsync(HttpMethod.Post, "/api/memory", new { text = " " })).StatusCode);
    }

    [Fact]
    public async Task RememberedFacts_ReachTheModel_ForPhoneChatsToo()
    {
        await using ServerRig rig = await ServerRig.StartAsync();
        await rig.SendAsync(HttpMethod.Post, "/api/memory", new { text = "My sister is called Ola" });

        await rig.ChatAsync("what is my sister called?");

        Assert.Contains("Ola", rig.Ai.LastPromptText);
    }
}
