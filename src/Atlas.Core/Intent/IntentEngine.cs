using System.Text.RegularExpressions;

namespace Atlas.Core.Intent;

public class IntentEngine
{
    public IntentResult Detect(string input)
    {
        string text = input.Trim().ToLower();

        IntentType type = text switch
        {
            var t when t.StartsWith("/")
                => IntentType.Command,

            var t when IsMath(t)
                => IntentType.Tool,

            var t when
                t.Contains("economy") ||
                t.Contains("stock") ||
                t.Contains("market") ||
                t.Contains("investment") ||
                t.Contains("bitcoin") ||
                t.Contains("crypto") ||
                t.Contains("currency") ||
                t.Contains("inflation") ||
                t.Contains("bank")
                => IntentType.Finance,

            var t when
                t.Contains("light") ||
                t.Contains("fan") ||
                t.Contains("ac") ||
                t.Contains("television") ||
                t.Contains("lamp") ||
                t.Contains("door") ||
                t.Contains("garage") ||
                t.Contains("thermostat") ||
                t.Contains("curtain")
                => IntentType.SmartHome,

            var t when
                t.Contains("code") ||
                t.Contains("program") ||
                t.Contains("bug") ||
                t.Contains("compile") ||
                t.Contains("debug") ||
                t.Contains("function") ||
                t.Contains("class") ||
                t.Contains("method")
                => IntentType.Coding,

            var t when
                t.Contains("research") ||
                t.Contains("search") ||
                t.Contains("find") ||
                t.Contains("look up")
                => IntentType.Research,

            var t when
                t.Contains("automate") ||
                t.Contains("schedule") ||
                t.Contains("remind")
                => IntentType.Automation,

            _ => IntentType.Conversation
        };

        double confidence = type switch
        {
            IntentType.Command => 1.00,
            IntentType.Tool => 0.95,
            IntentType.Finance => 0.90,
            IntentType.SmartHome => 0.90,
            IntentType.Coding => 0.85,
            IntentType.Research => 0.85,
            IntentType.Automation => 0.85,
            _ => 0.50
        };

        return new IntentResult
        {
            Type = type,
            Confidence = confidence,
            OriginalInput = input
        };
    }

    private static bool IsMath(string text)
    {
        string[] keywords =
        {
            "calculate",
            "solve",
            "add",
            "subtract",
            "multiply",
            "divide",
            "average",
            "mean",
            "median",
            "mode",
            "percentage",
            "percent",
            "square",
            "cube",
            "root",
            "sqrt",
            "pythagoras",
            "triangle",
            "circle",
            "radius",
            "diameter",
            "area",
            "perimeter",
            "volume",
            "equation",
            "algebra",
            "geometry",
            "statistics",
            "finance",
            "interest",
            "loan",
            "profit",
            "convert"
        };

        if (keywords.Any(text.Contains))
            return true;

        // Detect expressions like:
        // 2+2
        // 15 * 8
        // (10+5)/3
       return Regex.IsMatch(
    text,
    @"^\s*[\d\+\-\*/\^\(\)\.= ]+\s*$");
    }
}