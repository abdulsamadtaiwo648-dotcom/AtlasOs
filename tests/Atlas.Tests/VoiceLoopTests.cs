using System.Runtime.CompilerServices;
using Atlas.Speech.Loop;

namespace Atlas.Tests;

public class VoiceLoopTests
{
    private sealed class FakeWake : IWakeSource
    {
        public int Wakes;
        public int Max;

        public Task WaitForWakeAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Wakes++;
            if (Wakes > Max)
                return Task.Delay(Timeout.Infinite, ct); // no more wake words
            return Task.CompletedTask;
        }
    }

    private sealed class FakeListener : IListener
    {
        private readonly Queue<string> _utterances;
        public FakeListener(params string[] utterances) => _utterances = new(utterances);
        public Task<string> ListenAsync(CancellationToken ct)
            => Task.FromResult(_utterances.Count > 0 ? _utterances.Dequeue() : "");
    }

    private sealed class FakeSpeaker : ISpeaker
    {
        public List<string> Spoken = new();
        public Task SpeakAsync(string text, CancellationToken ct)
        {
            Spoken.Add(text);
            return Task.CompletedTask;
        }
    }

    private static async IAsyncEnumerable<string> Reply(
        string text, [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (string piece in text.Split('|'))
        {
            await Task.Yield();
            yield return piece;
        }
    }

    private static async Task RunUntilIdleAsync(VoiceLoop loop, FakeWake wake)
    {
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
        Task run = loop.RunAsync(cts.Token);

        // wait until the loop is waiting for a wake word that never comes
        while (wake.Wakes <= wake.Max && !run.IsCompleted)
            await Task.Delay(10);
        await Task.Delay(50);

        cts.Cancel();
        await run;
    }

    [Fact]
    public async Task WakeThenSpeech_IsAnsweredSentenceBySentence()
    {
        FakeWake wake = new() { Max = 1 };
        FakeSpeaker speaker = new();
        List<string> heard = new();

        VoiceLoop loop = new(
            wake, new FakeListener("what is the time"), speaker,
            (text, ct) => Reply("It is half past ten in the morning. |Have a nice day ahead of you."));
        loop.Heard += heard.Add;

        await RunUntilIdleAsync(loop, wake);

        Assert.Equal(new[] { "what is the time" }, heard);
        Assert.Equal(
            new[] { "It is half past ten in the morning.", "Have a nice day ahead of you." },
            speaker.Spoken);
        Assert.Equal(VoiceState.Stopped, loop.State);
    }

    [Fact]
    public async Task EmptyTranscript_IsIgnoredAndLoopReturnsToIdle()
    {
        FakeWake wake = new() { Max = 1 };
        FakeSpeaker speaker = new();
        bool responded = false;

        VoiceLoop loop = new(
            wake, new FakeListener(""), speaker,
            (text, ct) => { responded = true; return Reply("never"); });

        await RunUntilIdleAsync(loop, wake);

        Assert.False(responded);
        Assert.Empty(speaker.Spoken);
    }

    [Fact]
    public async Task StopPhrase_EndsTheLoopWithGoodbye()
    {
        FakeWake wake = new() { Max = 5 };
        FakeSpeaker speaker = new();

        VoiceLoop loop = new(wake, new FakeListener("Stop listening."), speaker, (t, ct) => Reply("unused"));

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
        await loop.RunAsync(cts.Token);

        Assert.Equal(new[] { "Goodbye." }, speaker.Spoken);
        Assert.Equal(VoiceState.Stopped, loop.State);
        Assert.Equal(1, wake.Wakes);
    }

    [Fact]
    public async Task FailureInAReply_IsReportedAndLoopKeepsRunning()
    {
        FakeWake wake = new() { Max = 2 };
        FakeSpeaker speaker = new();
        List<Exception> errors = new();
        int calls = 0;

        VoiceLoop loop = new(
            wake, new FakeListener("first", "second"), speaker,
            (text, ct) =>
            {
                calls++;
                return calls == 1 ? Failing() : Reply("All good now, thanks for asking.");
            });
        loop.Error += errors.Add;

        await RunUntilIdleAsync(loop, wake);

        Assert.Single(errors);
        Assert.Contains("Sorry, something went wrong.", speaker.Spoken);
        Assert.Contains("All good now, thanks for asking.", speaker.Spoken);

        static async IAsyncEnumerable<string> Failing()
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }

    [Theory]
    [InlineData("stop listening", true)]
    [InlineData("Goodbye!", true)]
    [InlineData("Goodbye, Atlas.", true)]
    [InlineData("say goodbye to my friend", false)]
    [InlineData("what's the weather", false)]
    public void StopPhraseDetection(string text, bool expected)
    {
        Assert.Equal(expected, VoiceLoop.IsStopPhrase(text));
    }
}
