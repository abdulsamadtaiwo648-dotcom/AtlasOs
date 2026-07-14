using System.Text.Json;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.Core.Services;

public class JsonConversationStore : IConversationStore
{
    private readonly string _folder = "Data";

    public JsonConversationStore()
    {
        Directory.CreateDirectory(_folder);
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
            string json = File.ReadAllText(file);

            Conversation? conversation =
                JsonSerializer.Deserialize<Conversation>(json);

            if (conversation != null)
                conversations.Add(conversation);
        }

        return conversations;
    }
}