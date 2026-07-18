using Atlas.Core.Intent;
using Atlas.Core.Planning;

namespace Atlas.Core.Services;

public class ThinkingEngine
{
    public ThinkingResult Think(
        string input,
        IntentResult intent,
        Plan plan)
    {
        ThinkingResult result = new();

        result.Input = input;
        result.Intent = intent.Type;
        result.Plan = plan;

        switch (intent.Type)
        {
            case IntentType.SmartHome:
                result.TargetEngine = "SmartHome";
                result.ShouldUseAI = false;
                break;

            case IntentType.System:
                result.TargetEngine = "System";
                result.ShouldUseAI = false;
                break;

            case IntentType.Vision:
                result.TargetEngine = "Vision";
                result.ShouldUseAI = false;
                break;

            case IntentType.Finance:
                result.TargetEngine = "Finance";
                result.ShouldUseAI = false;
                break;

            case IntentType.Tool:
                result.TargetEngine = "Calculator";
                result.ShouldUseAI = false;
                break;

            case IntentType.Coding:
                result.TargetEngine = "Conversation";
                result.ShouldUseAI = true;
                break;

            case IntentType.Research:
                result.TargetEngine = "Conversation";
                result.ShouldUseAI = true;
                break;

            default:
                result.TargetEngine = "Conversation";
                result.ShouldUseAI = true;
                break;
        }

        return result;
    }
}