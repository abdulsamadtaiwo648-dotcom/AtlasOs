using Atlas.Core.Planning;
using Atlas.Core.Intent;

namespace Atlas.Core.Services;

public class ThinkingResult
{
    public string Input { get; set; } = "";

    public IntentType Intent { get; set; }

    public Plan? Plan { get; set; }

    public string TargetEngine { get; set; } = "";

    public bool ShouldUseAI { get; set; }
}