using Atlas.Core.Commands;
using Atlas.Core.Enums;
using Atlas.Core.Interfaces;
using Atlas.Core.Memory;
using Atlas.Core.Models;
using Atlas.Core.Services;
using Atlas.Memory;

namespace Atlas.Tests;

public sealed class TempDb : IDisposable
{
    public string Path { get; } = global::System.IO.Path.Combine(
        global::System.IO.Path.GetTempPath(), $"atlas_test_{Guid.NewGuid():N}.db");

    public SqliteMemoryService Open() => new(Path);

    public void Dispose()
    {
        foreach (string f in new[] { Path, Path + "-wal", Path + "-shm", Path + "-journal" })
            if (File.Exists(f)) File.Delete(f);
    }
}

public class SqliteMemoryServiceTests
{
    [Fact]
    public void Remember_StoresAndReadsBackEverything()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();

        MemoryEntry saved = memory.Remember(
            "My sister is called Ola", MemoryKind.Fact, new[] { "Family", "family", " people " }, importance: 4, source: "voice");

        MemoryEntry? loaded = memory.Get(saved.Id);

        Assert.NotNull(loaded);
        Assert.Equal("My sister is called Ola", loaded!.Content);
        Assert.Equal(MemoryKind.Fact, loaded.Kind);
        Assert.Equal(new[] { "family", "people" }, loaded.Tags);
        Assert.Equal(4, loaded.Importance);
        Assert.Equal("voice", loaded.Source);
        Assert.Equal(1, memory.Count());
    }

    [Fact]
    public void Remember_SameThingTwice_UpdatesInsteadOfDuplicating()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();

        MemoryEntry first = memory.Remember("I live in Lagos", importance: 2);
        MemoryEntry second = memory.Remember("i live in lagos", importance: 5);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(5, second.Importance);
        Assert.Equal(1, memory.Count());
    }

    [Fact]
    public void Remember_RejectsEmptyText()
    {
        using TempDb db = new();
        Assert.Throws<ArgumentException>(() => db.Open().Remember("   "));
    }

    [Fact]
    public void Search_FindsRelevantMemories_ByMeaningfulWords()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("My sister is called Ola");
        memory.Remember("I prefer dark mode in every app", MemoryKind.Preference);
        memory.Remember("The wifi password is on the fridge");

        IReadOnlyList<MemoryEntry> results = memory.Search("what is my sister's name?");

        Assert.Single(results);
        Assert.Equal("My sister is called Ola", results[0].Content);
    }

    [Fact]
    public void Search_UsesStemmingAndPrefixes()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("I really like running in the morning");

        Assert.Single(memory.Search("who runs every day"));
        Assert.Single(memory.Search("morn"));
    }

    [Fact]
    public void Search_RanksTheBestMatchFirst()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("I like coffee");
        memory.Remember("My favourite coffee shop is on Allen Avenue near the bank");

        IReadOnlyList<MemoryEntry> results = memory.Search("coffee shop avenue");

        Assert.Equal(2, results.Count);
        Assert.Contains("Allen Avenue", results[0].Content);
    }

    [Theory]
    [InlineData("")]
    [InlineData("what is the")]
    [InlineData("\"; DROP TABLE memories; --")]
    [InlineData("AND OR NOT * ( ) \"")]
    public void Search_HandlesEmptyOrHostileQueriesSafely(string query)
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("Safe data stays safe");

        IReadOnlyList<MemoryEntry> results = memory.Search(query);

        Assert.Empty(results);
        Assert.Equal(1, memory.Count());
    }

    [Fact]
    public void Search_RecordsAccess()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        MemoryEntry saved = memory.Remember("My passport expires in March");

        memory.Search("passport");
        memory.Search("passport");

        MemoryEntry loaded = memory.Get(saved.Id)!;
        Assert.Equal(2, loaded.AccessCount);
        Assert.NotNull(loaded.LastAccessedAt);
    }

    [Fact]
    public void Forget_RemovesFromListAndSearch()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        MemoryEntry saved = memory.Remember("My locker code is 4821");

        Assert.True(memory.Forget(saved.Id));

        Assert.Null(memory.Get(saved.Id));
        Assert.Empty(memory.Search("locker code"));
        Assert.False(memory.Forget(saved.Id));
    }

    [Fact]
    public void List_FiltersByKindAndPinnedReturnsImportantOnes()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("Likes jollof rice", MemoryKind.Preference);
        memory.Remember("Birthday is on the 3rd", MemoryKind.Fact, importance: 5);

        Assert.Single(memory.List(MemoryKind.Preference));
        Assert.Equal(2, memory.List().Count);
        Assert.Equal("Birthday is on the 3rd", Assert.Single(memory.Pinned()).Content);
    }

    [Fact]
    public void Data_SurvivesReopeningTheDatabase()
    {
        using TempDb db = new();
        db.Open().Remember("I support Arsenal");

        SqliteMemoryService reopened = db.Open();

        Assert.Equal(1, reopened.Count());
        Assert.Single(reopened.Search("Arsenal"));
    }
}

public class MemoryPhraseParserTests
{
    [Theory]
    [InlineData("remember that my sister is called Ola", MemoryAction.Remember, "my sister is called Ola")]
    [InlineData("Remember my locker code is 4821.", MemoryAction.Remember, "my locker code is 4821")]
    [InlineData("please keep in mind that I work late on Fridays", MemoryAction.Remember, "I work late on Fridays")]
    [InlineData("Atlas, remember that I'm allergic to peanuts", MemoryAction.Remember, "I'm allergic to peanuts")]
    [InlineData("forget my locker code", MemoryAction.Forget, "my locker code")]
    [InlineData("forget about the meeting", MemoryAction.Forget, "the meeting")]
    [InlineData("what do you remember about my sister?", MemoryAction.RecallTopic, "my sister")]
    [InlineData("do you remember about the trip", MemoryAction.RecallTopic, "the trip")]
    [InlineData("what do you know about me", MemoryAction.RecallAll, "")]
    [InlineData("what do you remember?", MemoryAction.RecallAll, "")]
    [InlineData("show my memories", MemoryAction.RecallAll, "")]
    public void Parse_RecognisesMemoryPhrases(string input, MemoryAction action, string text)
    {
        MemoryRequest? request = MemoryPhraseParser.Parse(input);

        Assert.NotNull(request);
        Assert.Equal(action, request!.Action);
        Assert.Equal(text, request.Text);
    }

    [Theory]
    [InlineData("hello there")]
    [InlineData("forget it")]
    [InlineData("I can't remember where I put my keys")]
    [InlineData("/memory")]
    [InlineData("")]
    [InlineData("turn on the light")]
    public void Parse_IgnoresEverythingElse(string input)
    {
        Assert.Null(MemoryPhraseParser.Parse(input));
    }

    [Fact]
    public void Parse_FlagsPreferences()
    {
        Assert.Equal(MemoryKind.Preference, MemoryPhraseParser.Parse("remember that I love jollof rice")!.Kind);
        Assert.Equal(MemoryKind.Preference, MemoryPhraseParser.Parse("remember my favourite colour is blue")!.Kind);
        Assert.Equal(MemoryKind.Fact, MemoryPhraseParser.Parse("remember that my flight is at 9")!.Kind);
    }
}

public class MemoryHandlerAndCommandTests
{
    [Fact]
    public void RememberThenRecallThenForget_ThroughSpokenPhrases()
    {
        using TempDb db = new();
        MemoryHandler handler = new(db.Open());

        string saved = handler.Handle(MemoryPhraseParser.Parse("remember that my sister is called Ola")!);
        Assert.Contains("remember that", saved);

        string again = handler.Handle(MemoryPhraseParser.Parse("remember that my sister is called Ola")!);
        Assert.Contains("already knew", again);

        string recalled = handler.Handle(MemoryPhraseParser.Parse("what do you remember about my sister")!);
        Assert.Contains("Ola", recalled);

        string forgotten = handler.Handle(MemoryPhraseParser.Parse("forget my sister")!);
        Assert.Contains("forgotten", forgotten);

        string gone = handler.Handle(MemoryPhraseParser.Parse("what do you remember about my sister")!);
        Assert.Contains("don't have anything", gone);
    }

    [Fact]
    public void Forget_WithNoMatch_DeletesNothing()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("I like tea");

        string reply = new MemoryHandler(memory).Handle(MemoryPhraseParser.Parse("forget my locker code")!);

        Assert.Contains("don't have anything", reply);
        Assert.Equal(1, memory.Count());
    }

    [Fact]
    public void Slash_Commands_ListSearchAndForgetById()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        MemoryEntry tea = memory.Remember("I like tea");
        memory.Remember("My bike is red");

        MemoryCommand list = new(memory);
        Assert.Contains("I like tea", list.Execute(Array.Empty<string>()));
        Assert.Contains("My bike is red", list.Execute(Array.Empty<string>()));
        Assert.DoesNotContain("My bike is red", list.Execute(new[] { "search", "tea" }));

        ForgetCommand forget = new(memory);
        Assert.StartsWith("Usage", forget.Execute(Array.Empty<string>()));
        Assert.Contains("No memory with id", forget.Execute(new[] { "9999" }));
        Assert.Contains("Forgot: I like tea", forget.Execute(new[] { tea.Id.ToString() }));
        Assert.Equal(1, memory.Count());
    }
}

public class ConversationMemoryTests
{
    private sealed class CapturingAi : IAIProvider
    {
        public List<List<Message>> Prompts = new();

        public Task<string> GetResponseAsync(List<Message> messages)
        {
            Prompts.Add(messages.ToList());
            return Task.FromResult("ok");
        }
    }

    private sealed class NullStore : IConversationStore
    {
        public Conversation Create() => new();
        public void Save(Conversation conversation) { }
        public Conversation? Load(Guid id) => null;
        public void Delete(Guid id) { }
        public List<Conversation> GetAll() => new();
    }

    [Fact]
    public async Task RelevantMemories_AreAddedToThePrompt_ButNotSavedInHistory()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("My sister is called Ola");
        memory.Remember("I support Arsenal");

        CapturingAi ai = new();
        ConversationService service = new(ai, new NullStore(), memory);

        await service.SendMessageAsync("what is my sister's name?");

        List<Message> prompt = ai.Prompts.Single();
        Message note = prompt.First();
        Assert.Equal(MessageRole.System, note.Role);
        Assert.Contains("Ola", note.Content);
        Assert.DoesNotContain("Arsenal", note.Content);
        Assert.Equal("what is my sister's name?", prompt.Last().Content);

        Assert.DoesNotContain(service.GetConversation().Messages, m => m.Role == MessageRole.System);
    }

    [Fact]
    public async Task PinnedMemories_AreAlwaysIncluded()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("I am allergic to peanuts", importance: 5);

        CapturingAi ai = new();
        await new ConversationService(ai, new NullStore(), memory).SendMessageAsync("recommend a snack");

        Assert.Contains("peanuts", ai.Prompts.Single().First().Content);
    }

    [Fact]
    public async Task NoRelevantMemory_LeavesThePromptUntouched()
    {
        using TempDb db = new();
        SqliteMemoryService memory = db.Open();
        memory.Remember("I support Arsenal");

        CapturingAi ai = new();
        await new ConversationService(ai, new NullStore(), memory).SendMessageAsync("tell me a joke");

        Assert.All(ai.Prompts.Single(), m => Assert.NotEqual(MessageRole.System, m.Role));
    }
}
