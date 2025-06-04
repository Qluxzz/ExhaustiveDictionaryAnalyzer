namespace ExhaustiveDictionary.App;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
sealed class ExhaustiveAttribute : Attribute
{
    public ExhaustiveAttribute() { }
}

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
