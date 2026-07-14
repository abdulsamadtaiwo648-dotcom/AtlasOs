namespace Atlas.Math.Statistics;

public class StatisticsEngine
{
    public double Mean(IEnumerable<double> numbers)
    {
        if (!numbers.Any())
            throw new ArgumentException("Collection is empty.");

        return numbers.Average();
    }

    public double Sum(IEnumerable<double> numbers)
    {
        return numbers.Sum();
    }

    public double Max(IEnumerable<double> numbers)
    {
        return numbers.Max();
    }

    public double Min(IEnumerable<double> numbers)
    {
        return numbers.Min();
    }

    public double Range(IEnumerable<double> numbers)
    {
        return Max(numbers) - Min(numbers);
    }

    public double Median(List<double> numbers)
    {
        if (numbers.Count == 0)
            throw new ArgumentException("Collection is empty.");

        numbers.Sort();

        int middle = numbers.Count / 2;

        if (numbers.Count % 2 == 0)
            return (numbers[middle - 1] + numbers[middle]) / 2;

        return numbers[middle];
    }
}