using System.Text;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Engines;

public class ChartEngine
{
    // Sparkline characters from low to high
    private static readonly string[] SparkBars = { " ", "▂", "▃", "▄", "▅", "▆", "▇", "█" };

    /// <summary>
    /// Generates a sparkline chart for the given historical prices.
    /// </summary>
    public string GenerateSparkline(List<HistoricalPrice> prices)
    {
        if (prices == null || prices.Count == 0) return "";

        decimal min = prices.Min(p => p.Price);
        decimal max = prices.Max(p => p.Price);

        if (min == max)
        {
            return new string('▄', prices.Count);
        }

        var sb = new StringBuilder();
        foreach (var price in prices)
        {
            // Normalize to 0 - (SparkBars.Length - 1)
            int index = (int)Math.Round((price.Price - min) / (max - min) * (SparkBars.Length - 1));
            
            // Clamp index just in case of rounding issues
            if (index < 0) index = 0;
            if (index >= SparkBars.Length) index = SparkBars.Length - 1;

            sb.Append(SparkBars[index]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Calculates the Simple Moving Average (SMA) for the given prices.
    /// </summary>
    public decimal CalculateSMA(List<HistoricalPrice> prices, int period)
    {
        if (prices == null || prices.Count == 0 || period <= 0) return 0;
        
        // Take the last 'period' elements
        var subset = prices.Skip(Math.Max(0, prices.Count - period)).ToList();
        if (subset.Count == 0) return 0;

        return subset.Average(p => p.Price);
    }
}
