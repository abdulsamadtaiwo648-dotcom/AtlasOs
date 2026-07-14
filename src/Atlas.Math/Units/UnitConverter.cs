namespace Atlas.Math.Units;

public class UnitConverter
{
    public double KmToMiles(double km)
    {
        return km * 0.621371;
    }

    public double MilesToKm(double miles)
    {
        return miles / 0.621371;
    }

    public double CelsiusToFahrenheit(double celsius)
    {
        return celsius * 9 / 5 + 32;
    }

    public double FahrenheitToCelsius(double fahrenheit)
    {
        return (fahrenheit - 32) * 5 / 9;
    }

    public double KgToPounds(double kg)
    {
        return kg * 2.20462;
    }

    public double PoundsToKg(double pounds)
    {
        return pounds / 2.20462;
    }

    public double MetersToFeet(double meters)
    {
        return meters * 3.28084;
    }

    public double FeetToMeters(double feet)
    {
        return feet / 3.28084;
    }
}