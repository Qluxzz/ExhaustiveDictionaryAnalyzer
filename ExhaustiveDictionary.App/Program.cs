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
    public static readonly List<Color> ColorToHex = new() { Color.Red };

    public static void Main()
    {
        Console.WriteLine(ColorToHex);
    }
}
