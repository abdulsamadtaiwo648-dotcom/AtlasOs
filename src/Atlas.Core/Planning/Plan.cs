namespace Atlas.Core.Planning;

public class Plan
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Goal { get; set; } = "";

    public List<PlanStep> Steps { get; set; } = new();

    public bool Completed { get; set; }
}