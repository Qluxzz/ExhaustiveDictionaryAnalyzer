using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ExhaustiveAnalyzer.Analyzer.EnumDictionaryAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveAnalyzer.Tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var expected = Verify
            .Diagnostic()
            .WithSpan(18, 47, 18, 57)
            .WithArguments("ColorToHex", "Program.Color.Green, Program.Color.Blue");

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color
    {
        Red,
        Green,
        Blue,
    };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" }
    };

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
",
            expected
        );
    }
}
