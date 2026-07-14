namespace Atlas.Core.Planning;

public class PlanStep
{
    public int Order { get; set; }

    public string Description { get; set; } = "";

    public bool Completed { get; set; }
}