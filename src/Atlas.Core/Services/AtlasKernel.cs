using System.Runtime.CompilerServices;
using Atlas.Core.Interfaces;
using Atlas.Core.Memory;
using Atlas.Core.Intent;
using Atlas.Core.Planning;
using Atlas.Tools.Services;
using Atlas.Finance.Interfaces;
using Atlas.SmartHome.Services;
using Atlas.System.Services;
using Atlas.Vision.Services;

namespace Atlas.Core.Services;

public class AtlasKernel : IAtlasKernel
{
    private readonly CommandDispatcher _dispatcher;
    private readonly ToolManager _toolManager;
    private readonly IConversationService _conversation;
    private readonly IPlannerEngine _planner;
    private readonly IntentEngine _intent;
    private readonly ThinkingEngine _thinking;
    private readonly IFinanceEngine _finance;
    private readonly SmartHomeEngine _smartHome;
    private readonly AppLauncherEngine _appLauncher;
    private readonly VisionEngine _vision;
    private readonly MemoryHandler? _memoryHandler;

    public AtlasKernel(
        IConversationService conversation,
        CommandDispatcher dispatcher,
        ToolManager toolManager,
        IPlannerEngine planner,
        IntentEngine intent,
        ThinkingEngine thinking,
        IFinanceEngine finance,
        SmartHomeEngine smartHome,
        AppLauncherEngine appLauncher,
        VisionEngine vision,
        IMemoryService? memory = null)
    {
        _conversation = conversation;
        _dispatcher = dispatcher;
        _toolManager = toolManager;
        _planner = planner;
        _intent = intent;
        _thinking = thinking;
        _finance = finance;
        _smartHome = smartHome;
        _appLauncher = appLauncher;
        _vision = vision;
        _memoryHandler = memory == null ? null : new MemoryHandler(memory);
    }

    public async Task<string> ProcessAsync(string input)
    {
        (string? text, string? chatPrompt) = await ResolveAsync(input);

        return text ?? await _conversation.SendMessageAsync(chatPrompt!);
    }

    public async IAsyncEnumerable<string> ProcessStreamAsync(
        string input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (string? text, string? chatPrompt) = await ResolveAsync(input);

        if (text != null)
        {
            yield return text;
            yield break;
        }

        await foreach (string chunk in _conversation.StreamMessageAsync(chatPrompt!, cancellationToken))
            yield return chunk;
    }

    /// <summary>
    /// Decides how to handle the input. Returns either a finished reply (text)
    /// or a prompt that should be sent to the AI conversation (chatPrompt).
    /// </summary>
    private async Task<(string? text, string? chatPrompt)> ResolveAsync(string input)
    {
        // Commands
        if (_dispatcher.TryExecute(input, out string command))
            return (command, null);

        // Memory ("remember that...", "forget...", "what do you remember...")
        if (_memoryHandler != null && MemoryPhraseParser.Parse(input) is MemoryRequest memoryRequest)
            return (_memoryHandler.Handle(memoryRequest), null);

        // Simple tools
        if (_toolManager.TryExecute(input, out string tool))
            return (tool, null);

        // Think
        IntentResult intent = _intent.Detect(input);

        Plan plan = _planner.CreatePlan(input);

        ThinkingResult thought =
            _thinking.Think(input, intent, plan);

        switch (thought.TargetEngine)
        {
            case "SmartHome":
                return (await _smartHome.ProcessCommandAsync(input), null);

            case "System":
                return (await _appLauncher.ProcessCommandAsync(input), null);

            case "Vision":
                return (await _vision.ProcessCommandAsync(input), null);

            case "Finance":
            {
                string context =
                    await _finance.BuildContextAsync(input);

                string prompt =
$"""
The user asked:

{input}

Use the following live market data.

{context}

Analyze it like a professional financial analyst.

Explain:

- Current trend
- Risks
- Opportunities
- Short-term outlook
- Medium-term outlook
- Your reasoning
- show the market in full

Use only the supplied market data.
""";

                return (null, prompt);
            }

            case "Calculator":
                if (_toolManager.TryExecute(input, out string calculator))
                    return (calculator, null);

                return ("Calculator could not process the request.", null);

            case "Conversation":
                return (null, input);

            default:
                return ("I don't know how to handle that yet.", null);
        }
    }
}
