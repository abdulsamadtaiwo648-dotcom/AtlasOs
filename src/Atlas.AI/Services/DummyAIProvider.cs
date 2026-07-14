using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.AI.Services;

public class DummyAIProvider : IAIProvider
{
    public Task<string> GetResponseAsync(List<Message> messages)
    {
        string input = messages.Last().Content
                               .ToLower()
                               .Trim();

        string response = input switch
        {
            "hello" => "Hello Wonder! I'm Atlas.",
            "hi" => "Hi Wonder!",
            "how are you" => "I'm doing great!",
            _ => "I'm still learning."
        };

        return Task.FromResult(response);
    }
}