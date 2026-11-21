using System.Text.Json;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.Core.Services;

public class JsonConversationStore : IConversationStore
{
    private readonly string _folder;

    /// <summary>
    /// Chats are kept in one fixed folder (ATLAS_DATA, or conversations under the user's local
    /// app-data) so the desktop app and the phone server see the same history. Chats from the
    /// old relative "Data" folder are copied over once.
    /// </summary>
    public JsonConversationStore(string? folder = null)
    {
        _folder = folder ?? DefaultFolder();
        Directory.CreateDirectory(_folder);

        if (folder == null)
            ImportLegacyFolder("Data");
    }

    public static string DefaultFolder()
    {
        string? env = Environment.GetEnvironmentVariable("ATLAS_DATA");
        if (!string.IsNullOrWhiteSpace(env))
            return env;

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtlasOS",
            "conversations");
    }

    private void ImportLegacyFolder(string legacy)
    {
        try
        {
            if (!Directory.Exists(legacy) || Directory.EnumerateFiles(_folder, "*.json").Any())
                return;

            foreach (string file in Directory.GetFiles(legacy, "*.json"))
                File.Copy(file, Path.Combine(_folder, Path.GetFileName(file)), overwrite: false);
        }
        catch (Exception)
        {
            // Importing old chats is a convenience; never block start-up on it.
        }
    }

    public Conversation Create()
    {
        return new Conversation();
    }

    public void Save(Conversation conversation)
    {
        string path = Path.Combine(_folder, $"{conversation.Id}.json");

        string json = JsonSerializer.Serialize(
            conversation,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(path, json);
    }

    public Conversation? Load(Guid id)
    {
        string path = Path.Combine(_folder, $"{id}.json");

        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<Conversation>(json);
    }

    public void Delete(Guid id)
    {
        string path = Path.Combine(_folder, $"{id}.json");

        if (File.Exists(path))
            File.Delete(path);
    }

    public List<Conversation> GetAll()
    {
        List<Conversation> conversations = new();

        foreach (string file in Directory.GetFiles(_folder, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);

                Conversation? conversation =
                    JsonSerializer.Deserialize<Conversation>(json);

                if (conversation != null)
                    conversations.Add(conversation);
            }
            catch (Exception)
            {
                // One damaged file must not hide every other chat.
            }
        }

        return conversations;
    }
}