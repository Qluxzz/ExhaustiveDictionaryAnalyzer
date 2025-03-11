# Exhaustive Dictionary Analyzer

## Rules:

### EXHAUSTIVEDICT0001

Ensures all enum values are defined in dictionaries with an enum type as the key, when attributed with the `[Exhaustive]` attribute

```csharp
enum Color { Red, Green, Blue };

[Exhaustive]
Dictionary<Color, string> ColorToHex = new() {
    { Color.Red, "#FF0000" }
};
```

Here the rule will complain because we have forgotten to add a value for `Color.Green` and `Color.Blue` to the dictionary

### EXHAUSTIVEDICT0002

Ensure only one value per enum key is defined in the dictionary

```csharp
enum Color { Red, Green, Blue };

[Exhaustive]
Dictionary<Color, string> ColorToHex = new() {
    { Color.Red, "#FF0000" },
    { Color.Green, "#008000" },
    { Color.Blue, "#0000FF" },
    { Color.Red, "#FF0000" },
};
```

Here the rule will complain because we have added `Color.Red` twice
