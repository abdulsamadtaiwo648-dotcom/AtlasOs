namespace Atlas.Core.Routing;

public interface IRequestRouter
{
    RouteResult Route(string input);
}