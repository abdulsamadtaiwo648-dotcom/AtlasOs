using Atlas.Core.Planning;

namespace Atlas.Core.Interfaces;

public interface IPlannerEngine
{
    Plan CreatePlan(string goal);
}