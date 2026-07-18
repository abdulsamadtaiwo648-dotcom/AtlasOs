using Atlas.Core.Interfaces;
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
        VisionEngine vision)
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
    }

    public async Task<string> ProcessAsync(string input)
    {
        // Commands
        if (_dispatcher.TryExecute(input, out string command))
            return command;

        // Simple tools
        if (_toolManager.TryExecute(input, out string tool))
            return tool;

        // Think
        IntentResult intent = _intent.Detect(input);

        Plan plan = _planner.CreatePlan(input);

        ThinkingResult thought =
            _thinking.Think(input, intent, plan);

        switch (thought.TargetEngine)
        {
           case "SmartHome":
                return await _smartHome.ProcessCommandAsync(input);

           case "System":
                return await _appLauncher.ProcessCommandAsync(input);

           case "Vision":
                return await _vision.ProcessCommandAsync(input);

           case "Finance":

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

    return await _conversation.SendMessageAsync(prompt);
            case "Calculator":
                if (_toolManager.TryExecute(input, out string calculator))
                    return calculator;

                return "Calculator could not process the request.";

            case "Conversation":
                return await _conversation.SendMessageAsync(input);

            default:
                return "I don't know how to handle that yet.";
        }
    }
}