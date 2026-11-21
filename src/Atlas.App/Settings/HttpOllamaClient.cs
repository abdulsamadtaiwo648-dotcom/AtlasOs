using System.Text.Json;

namespace Atlas.App.Settings;

public sealed class HttpOllamaClient : IOllamaClient
{
    private readonly HttpClient _http;

    public HttpOllamaClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<string>> ListModelsAsync(string baseUrl, CancellationToken cancellationToken)
    {
        string url = baseUrl.TrimEnd('/') + "/api/tags";

        using HttpResponseMessage response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        List<string> names = new();
        if (json.RootElement.TryGetProperty("models", out JsonElement models))
        {
            foreach (JsonElement model in models.EnumerateArray())
            {
                if (model.TryGetProperty("name", out JsonElement name) && name.GetString() is string text)
                    names.Add(text);
            }
        }

        return names;
    }
}
