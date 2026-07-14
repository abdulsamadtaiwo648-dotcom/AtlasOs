namespace Atlas.Core.Intent;

public class IntentResult
{
    public IntentType Type { get; set; }

    public double Confidence { get; set; }

    public string OriginalInput { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
}