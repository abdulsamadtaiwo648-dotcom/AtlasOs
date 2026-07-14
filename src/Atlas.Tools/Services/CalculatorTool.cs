using Atlas.Tools.Interfaces;
using System.Data;

namespace Atlas.Tools.Services;

public class CalculatorTool : ITool
{
    public string Name => "Calculator";

    public bool CanHandle(string input)
    {
        return input.StartsWith("calculate ",
            StringComparison.OrdinalIgnoreCase);
    }

    public string Execute(string input)
    {
        string expression = input[10..];

        try
        {
            object result = new DataTable().Compute(expression, "");

            return result.ToString() ?? "";
        }
        catch
        {
            return "Invalid expression.";
        }
    }
}