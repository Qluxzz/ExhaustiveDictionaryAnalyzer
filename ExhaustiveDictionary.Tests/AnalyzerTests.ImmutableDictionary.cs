using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

public sealed partial class AnalyzerTests
{
    [TestMethod]
    public async Task ReportsMissingValuesInImmutableDictionaryOnProperty()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 38, 11, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static ImmutableDictionary<Color, string> ColorToHex =
        new Dictionary<Color, string>() { { Color.Red, ""#FF0000"" } }.ToImmutableDictionary();
}
",
            expected
        );
    }
}
