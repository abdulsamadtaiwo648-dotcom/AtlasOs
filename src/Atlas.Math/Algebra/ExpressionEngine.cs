using System.Data;

namespace Atlas.Math.Algebra;

public class ExpressionEngine
{
    private readonly DataTable _dataTable;

    public ExpressionEngine()
    {
        _dataTable = new DataTable();
    }

    public double Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be null or empty.");

        try
        {
            var result = _dataTable.Compute(expression, string.Empty);
            return Convert.ToDouble(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate expression: {expression}. Error: {ex.Message}", ex);
        }
    }
}
