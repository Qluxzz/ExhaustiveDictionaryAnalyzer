namespace ExhaustiveDictionary.App;

public static class Program
{
    public enum Color
    {
        Red,
        Green,
        Blue,
    };

    [Exhaustive]
    public static readonly Dictionary<Color, string> ColorToHex = new() { { Color.Red, "red" } };

    public static void Main()
    {
        Console.WriteLine(ColorToHex);
    }
}
