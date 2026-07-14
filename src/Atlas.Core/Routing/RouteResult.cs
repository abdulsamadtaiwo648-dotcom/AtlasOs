namespace Atlas.Core.Routing;

public class RouteResult
{
    public bool IsCommand { get; set; }

    public bool IsTool { get; set; }

    public bool RequiresPlanning { get; set; }

    public bool RequiresAI { get; set; }
}