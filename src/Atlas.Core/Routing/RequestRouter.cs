namespace Atlas.Core.Routing;

public class RequestRouter : IRequestRouter
{
    public RouteResult Route(string input)
    {
        input = input.Trim();

        RouteResult result = new();

        if (input.StartsWith("/"))
        {
            result.IsCommand = true;
            return result;
        }

        if (input.StartsWith("calculate"))
        {
            result.IsTool = true;
            return result;
        }

        result.RequiresPlanning = true;
        result.RequiresAI = true;

        return result;
    }
}