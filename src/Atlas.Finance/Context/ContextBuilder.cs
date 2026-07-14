using System.Text;

namespace Atlas.Finance.Context;

public class ContextBuilder
{
    public string Build(
        string question,
        FinanceContext finance)
    {
        StringBuilder sb = new();

        sb.AppendLine($"User Question: {question}");
        sb.AppendLine();

        sb.AppendLine("Market Data");
        sb.AppendLine("--------------------------------");

        foreach (var coin in finance.Cryptocurrencies)
        {
            sb.AppendLine(
                $"{coin.Symbol}  " +
                $"Price: {coin.Price}  " +
                $"Change: {coin.ChangePercent}%");
        }

        sb.AppendLine();

        foreach (var stock in finance.Stocks)
        {
            sb.AppendLine(
                $"{stock.Symbol}  " +
                $"Price: {stock.Price}  " +
                $"Change: {stock.ChangePercent}%");
        }

        sb.AppendLine();

        foreach (var pair in finance.Forex)
        {
            sb.AppendLine(
                $"{pair.Symbol}  " +
                $"Price: {pair.Price}  " +
                $"Change: {pair.ChangePercent}%");
        }

        sb.AppendLine();

        sb.AppendLine("Economy");
        sb.AppendLine("--------------------------------");

        sb.AppendLine($"Interest Rate : {finance.Economy.InterestRate}%");
        sb.AppendLine($"Inflation     : {finance.Economy.InflationRate}%");
        sb.AppendLine($"GDP Growth    : {finance.Economy.GdpGrowth}%");

        return sb.ToString();
    }
}