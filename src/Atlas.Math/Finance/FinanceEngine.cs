namespace Atlas.Math.Finance;

public class FinanceEngine
{
    public double SimpleInterest(
        double principal,
        double rate,
        double years)
    {
        return (principal * rate * years) / 100;
    }

    public double CompoundInterest(
        double principal,
        double rate,
        double years)
    {
        return principal *
               System.Math.Pow(
                    1 + rate / 100,
                    years);
    }

    public double Profit(
        double sellingPrice,
        double costPrice)
    {
        return sellingPrice - costPrice;
    }

    public double ProfitPercentage(
        double sellingPrice,
        double costPrice)
    {
        return Profit(sellingPrice, costPrice)
               / costPrice * 100;
    }

    public double LoanPayment(
        double principal,
        double annualRate,
        int months)
    {
        double monthlyRate = annualRate / 12 / 100;

        return principal *
               monthlyRate *
               System.Math.Pow(1 + monthlyRate, months) /
               (System.Math.Pow(1 + monthlyRate, months) - 1);
    }
}