namespace Atlas.Math.Geometry;

public class GeometryEngine
{
    public double Pythagoras(double a, double b)
    {
        return System.Math.Sqrt(a * a + b * b);
    }

    public double CircleArea(double radius)
    {
        return System.Math.PI * radius * radius;
    }

    public double Circumference(double radius)
    {
        return 2 * System.Math.PI * radius;
    }
}