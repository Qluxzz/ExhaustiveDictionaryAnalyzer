using ExhaustiveAnalyzer.Analyzer;

namespace ExhaustiveAnalyzer.App;

public static class Program
{
    enum Color
    {
        Red,
        Green,
        Blue,
    };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() { { Color.Red, "#FF0000" } };

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
