namespace Atlas.Math.Algebra;

public class AlgebraEngine
{
    public double SolveLinear(double a, double b)
    {
        if (a == 0)
            throw new ArgumentException("a cannot be zero.");

        return -b / a;
    }
}