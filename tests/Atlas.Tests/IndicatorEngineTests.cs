using Atlas.Finance.Analysis.Technical;
using Atlas.Finance.Models;
using Atlas.Finance.Models.Technical;

namespace Atlas.Tests;

public class IndicatorEngineTests
{
    private static List<Candle> Trend(int count, decimal start, decimal step)
    {
        List<Candle> candles = new();
        for (int i = 0; i < count; i++)
        {
            decimal close = start + step * i;
            candles.Add(new Candle
            {
                Open = close - step / 2,
                High = close + 1,
                Low = close - 1,
                Close = close,
                Volume = 1000,
                Timestamp = new DateTime(2024, 1, 1).AddDays(i)
            });
        }
        return candles;
    }

    [Fact]
    public void Rsi_IsHighInUptrendAndLowInDowntrend()
    {
        Assert.True(IndicatorEngine.CalculateRSI(Trend(60, 100, 1)) > 70);
        Assert.True(IndicatorEngine.CalculateRSI(Trend(60, 200, -1)) < 30);
    }

    [Fact]
    public void Sma_AveragesLastPeriodCloses()
    {
        List<Candle> candles = Trend(10, 10, 1); // closes 10..19

        Assert.Equal(17m, IndicatorEngine.CalculateSMA(candles, 5)); // 15..19
    }

    [Fact]
    public void Obv_RisesWhenPriceRises()
    {
        Assert.Equal(9000m, IndicatorEngine.CalculateOBV(Trend(10, 10, 1)));
        Assert.Equal(-9000m, IndicatorEngine.CalculateOBV(Trend(10, 20, -1)));
    }

    [Fact]
    public void Obv_IsZeroWithTooFewCandles()
    {
        Assert.Equal(0m, IndicatorEngine.CalculateOBV(Trend(1, 10, 1)));
    }

    [Fact]
    public void Fibonacci_RetracementsLieBetweenLowAndHigh_ExtensionsBelowLow()
    {
        Dictionary<string, decimal> fib = IndicatorEngine.CalculateFibonacciRetracement(200m, 100m);

        Assert.Equal(200m, fib["0"]);
        Assert.Equal(100m, fib["100"]);
        Assert.Equal(150m, fib["50"]);
        foreach (string key in new[] { "23.6", "38.2", "50", "61.8", "78.6" })
            Assert.InRange(fib[key], 100m, 200m);
        Assert.True(fib["127.2"] < 100m);
        Assert.True(fib["161.8"] < fib["127.2"]);
    }

    [Fact]
    public void BuildIndicatorSet_FlagsGoldenCrossInSustainedUptrend()
    {
        IndicatorSet set = IndicatorEngine.BuildIndicatorSet(Trend(250, 100, 1));

        Assert.True(set.GoldenCross);
        Assert.False(set.DeathCross);
        Assert.Equal(349m, set.CurrentPrice);
    }

    [Fact]
    public void BuildIndicatorSet_FlagsDeathCrossInSustainedDowntrend()
    {
        IndicatorSet set = IndicatorEngine.BuildIndicatorSet(Trend(250, 500, -1));

        Assert.True(set.DeathCross);
        Assert.False(set.GoldenCross);
    }
}
