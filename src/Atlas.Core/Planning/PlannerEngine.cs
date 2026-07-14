using Atlas.Core.Interfaces;

namespace Atlas.Core.Planning;

public class PlannerEngine : IPlannerEngine
{
    public Plan CreatePlan(string goal)
    {
        Plan plan = new()
        {
            Goal = goal
        };

        plan.Steps.Add(new PlanStep
        {
            Order = 1,
            Description = "Understand the request"
        });

        plan.Steps.Add(new PlanStep
        {
            Order = 2,
            Description = "Choose the required tools"
        });

        plan.Steps.Add(new PlanStep
        {
            Order = 3,
            Description = "Execute the task"
        });

        plan.Steps.Add(new PlanStep
        {
            Order = 4,
            Description = "Verify the result"
        });

        return plan;
    }
}