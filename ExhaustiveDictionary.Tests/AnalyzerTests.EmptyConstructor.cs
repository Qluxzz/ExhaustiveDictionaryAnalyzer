using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

public sealed partial class AnalyzerTests
{
    [TestMethod]
    public async Task ReportsAllEnumValuesAsMissingWhenNotInitialized()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex;
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsAllEnumValuesAsMissingWhenUsingNewInitializer()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new();
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsAllEnumValuesAsMissingForExplicitTypeAndEmptyInitializer()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 55, 11, 65)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    private static readonly Dictionary<Color, string> ColorToHex = new Dictionary<Color, string>() { };
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsAllEnumValuesAsMissingWhenUsingEmptyCollectionExpression()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = [];
}
",
            expected
        );
    }
}
