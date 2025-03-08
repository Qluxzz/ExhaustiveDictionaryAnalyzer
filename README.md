# Exhaustive Analyzer

Allows you to ensure that dictionaries where the key is an enum type, that all enum values are defined in the dictionary.

## Example:

```csharp
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
```

Here the rule will complain that we have forgotten to add a value for `Color.Green` and `Color.Blue` to the dictionary
