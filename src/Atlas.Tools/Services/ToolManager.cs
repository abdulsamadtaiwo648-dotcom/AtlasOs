using Atlas.Tools.Interfaces;

namespace Atlas.Tools.Services;

public class ToolManager
{
    private readonly List<ITool> _tools;

    public ToolManager(IEnumerable<ITool> tools)
    {
        _tools = tools.ToList();
    }

    public bool TryExecute(string input, out string result)
    {
        foreach (ITool tool in _tools)
        {
            if (tool.CanHandle(input))
            {
                result = tool.Execute(input);
                return true;
            }
        }

        result = "";
        return false;
    }
}