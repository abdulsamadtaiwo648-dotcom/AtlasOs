using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Atlas.Core.Settings;
using Atlas.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Atlas.Server;

public static class AtlasServerApp
{
    public const int MaxMessageLength = 4000;

    /// <summary>
    /// Builds the phone server. <paramref name="configureBuilder"/> and <paramref name="beforePipeline"/>
    /// exist so tests can swap services and the transport; the real program passes neither.
    /// </summary>
    public static WebApplication Build(
        string[] args,
        ServerSettings settings,
        Action<WebApplicationBuilder>? configureBuilder = null,
        Action<IApplicationBuilder>? beforePipeline = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Services.AddAtlasCore();
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(new AuthThrottle());
        // Chats share one conversation inside Atlas, so only one request may run at a time.
        builder.Services.AddSingleton(new SemaphoreSlim(1, 1));

        builder.WebHost.ConfigureKestrel(k =>
        {
            k.AddServerHeader = false;
            k.Limits.MaxRequestBodySize = 32 * 1024;
        });

        configureBuilder?.Invoke(builder);

        WebApplication app = builder.Build();

        beforePipeline?.Invoke(app);

        // 1. Only private-network clients (home Wi-Fi, Tailscale) unless explicitly allowed.
        app.Use(async (context, next) =>
        {
            if (!NetworkPolicy.IsAllowed(context.Connection.RemoteIpAddress, settings.AllowPublicInternet))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Atlas only accepts connections from your private network.");
                return;
            }

            await next();
        });

        // 2. Safe-by-default headers.
        app.Use(async (context, next) =>
        {
            IHeaderDictionary h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["Referrer-Policy"] = "no-referrer";
            h["X-Frame-Options"] = "DENY";
            h["Content-Security-Policy"] =
                "default-src 'self'; img-src 'self' data:; connect-src 'self'; style-src 'self'; script-src 'self'; " +
                "frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

            if (context.Request.Path.StartsWithSegments("/api"))
                h["Cache-Control"] = "no-store";

            await next();
        });

        // 3. The password, required for everything under /api.
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await next();
                return;
            }

            AuthThrottle throttle = context.RequestServices.GetRequiredService<AuthThrottle>();
            string client = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (throttle.IsLockedOut(client))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsJsonAsync(new { error = "Too many wrong passwords. Try again in a few minutes." });
                return;
            }

            if (!TokenCheck.Matches(ReadToken(context.Request), settings.Token))
            {
                throttle.RecordFailure(client);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Wrong or missing password." });
                return;
            }

            throttle.RecordSuccess(client);
            await next();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles();

        MapApi(app);

        return app;
    }

    private static string? ReadToken(HttpRequest request)
    {
        string? header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        return !string.IsNullOrEmpty(header) && header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? header[prefix.Length..].Trim()
            : request.Headers["X-Atlas-Token"].ToString();
    }

    // ── API ────────────────────────────────────────────────────────────────

    private static void MapApi(WebApplication app)
    {
        RouteGroupBuilder api = app.MapGroup("/api");

        api.MapGet("/status", (ISettingsService settings, IMemoryService memory) => new
        {
            name = "Atlas",
            model = settings.Current.Model,
            memories = memory.Count()
        });

        // ── Chat ───────────────────────────────────────────────────────────

        api.MapGet("/chat", (IConversationService conversation) => ConversationDto(conversation.GetConversation()));

        api.MapPost("/chat", async (HttpContext http, ChatRequest request, IAtlasKernel kernel, SemaphoreSlim gate,
            ILoggerFactory logs) =>
        {
            string text = (request.Text ?? "").Trim();

            if (text.Length == 0)
                return Results.BadRequest(new { error = "Say something first." });

            if (text.Length > MaxMessageLength)
                return Results.BadRequest(new { error = $"That message is too long (limit {MaxMessageLength} characters)." });

            if (!await gate.WaitAsync(0))
                return Results.Json(new { error = "Atlas is busy with another request." }, statusCode: StatusCodes.Status409Conflict);

            try
            {
                http.Response.ContentType = "text/plain; charset=utf-8";
                http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

                try
                {
                    await foreach (string chunk in kernel.ProcessStreamAsync(text, http.RequestAborted))
                    {
                        await http.Response.WriteAsync(chunk, Encoding.UTF8, http.RequestAborted);
                        await http.Response.Body.FlushAsync(http.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                    // The phone went away or pressed stop.
                }
                catch (Exception ex)
                {
                    // Log the details here; send the phone a generic message only.
                    logs.CreateLogger("Atlas.Server").LogError(ex, "Chat request failed");

                    if (!http.Response.HasStarted || http.RequestAborted.IsCancellationRequested == false)
                        await http.Response.WriteAsync("\n\nSomething went wrong on the PC. Check the Atlas window for details.");
                }
            }
            finally
            {
                gate.Release();
            }

            return Results.Empty;
        });

        api.MapPost("/chat/new", (IConversationService conversation) =>
        {
            Conversation created = conversation.StartNew();
            return Results.Ok(new { id = created.Id });
        });

        // ── Chat history ───────────────────────────────────────────────────

        api.MapGet("/chats", (IConversationStore store, IConversationService conversation) =>
        {
            Guid current = conversation.GetConversation().Id;

            return store.GetAll()
                .Where(c => c.Messages.Any(m => m.Role == Atlas.Core.Enums.MessageRole.User))
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new { id = c.Id, title = c.Title(), createdAt = c.CreatedAt, current = c.Id == current });
        });

        api.MapPost("/chats/{id:guid}/open", (Guid id, IConversationService conversation) =>
            conversation.Load(id)
                ? Results.Ok(ConversationDto(conversation.GetConversation()))
                : Results.NotFound(new { error = "That chat no longer exists." }));

        api.MapDelete("/chats/{id:guid}", (Guid id, IConversationStore store, IConversationService conversation) =>
        {
            bool wasCurrent = conversation.GetConversation().Id == id;
            store.Delete(id);

            if (wasCurrent)
                conversation.StartNew();

            return Results.NoContent();
        });

        // ── Memory ─────────────────────────────────────────────────────────

        api.MapGet("/memory", (string? q, IMemoryService memory) =>
        {
            IReadOnlyList<MemoryEntry> entries = string.IsNullOrWhiteSpace(q)
                ? memory.List(limit: 200)
                : memory.Search(q, 50);

            return entries.Select(MemoryDto);
        });

        api.MapPost("/memory", (MemoryRequest request, IMemoryService memory) =>
        {
            string text = (request.Text ?? "").Trim();

            if (text.Length == 0)
                return Results.BadRequest(new { error = "Tell Atlas what to remember." });

            if (text.Length > MaxMessageLength)
                return Results.BadRequest(new { error = "That is too long to remember as one item." });

            int before = memory.Count();
            MemoryEntry entry = memory.Remember(text, source: "phone");

            return Results.Json(
                new { entry = MemoryDto(entry), alreadyKnown = memory.Count() == before },
                statusCode: StatusCodes.Status201Created);
        });

        api.MapPost("/memory/{id:long}/pin", (long id, PinRequest request, IMemoryService memory) =>
            memory.SetImportance(id, request.Pinned ? 5 : 3)
                ? Results.Ok(MemoryDto(memory.Get(id)!))
                : Results.NotFound(new { error = "No such memory." }));

        api.MapDelete("/memory/{id:long}", (long id, IMemoryService memory) =>
            memory.Forget(id) ? Results.NoContent() : Results.NotFound(new { error = "No such memory." }));
    }

    private static object ConversationDto(Conversation conversation) => new
    {
        id = conversation.Id,
        title = conversation.Title(),
        messages = conversation.Messages
            .Where(m => m.Role != Atlas.Core.Enums.MessageRole.System)
            .Select(m => new
            {
                role = m.Role == Atlas.Core.Enums.MessageRole.User ? "user" : "assistant",
                text = m.Content,
                time = m.Timestamp
            })
    };

    private static object MemoryDto(MemoryEntry e) => new
    {
        id = e.Id,
        content = e.Content,
        kind = e.Kind.ToString(),
        pinned = e.Importance >= 5,
        updatedAt = e.UpdatedAt
    };

    public sealed record ChatRequest(string? Text);
    public sealed record MemoryRequest(string? Text);
    public sealed record PinRequest(bool Pinned);

    /// <summary>Addresses the phone can use to reach this PC, for the start-up banner.</summary>
    public static IReadOnlyList<string> LocalAddresses()
    {
        List<string> result = new();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up
                || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var unicast in nic.GetIPProperties().UnicastAddresses)
            {
                IPAddress ip = unicast.Address;
                if (ip.AddressFamily == AddressFamily.InterNetwork
                    && NetworkPolicy.IsAllowed(ip, allowPublicInternet: false))
                    result.Add(ip.ToString());
            }
        }

        return result;
    }
}
