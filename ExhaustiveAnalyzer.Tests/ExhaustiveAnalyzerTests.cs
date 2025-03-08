using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    ExhaustiveAnalyzer.Analyzer.EnumDictionaryAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveAnalyzer.Tests;

[TestClass]
public sealed class ExhaustiveAnalyzerTests
{
    [TestMethod]
    public async Task ReportsMissingValuesInDictionary()
    {
        var expected = Verify
            .Diagnostic()
            .WithSpan(16, 47, 16, 57)
            .WithArguments("ColorToHex", "Program.Color.Green, Program.Color.Blue");

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TestAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive, Test]
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

    [TestMethod]
    public async Task ReportsAllEnumValuesAsMissingWhenUsingEmptyCollectionExpression()
    {
        var expected = Verify
            .Diagnostic()
            .WithSpan(13, 47, 13, 57)
            .WithArguments(
                "ColorToHex",
                "Program.Color.Red, Program.Color.Green, Program.Color.Blue"
            );

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = [];

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsAllEnumValuesAsMissingWhenNotInitialized()
    {
        var expected = Verify
            .Diagnostic()
            .WithSpan(13, 47, 13, 57)
            .WithArguments(
                "ColorToHex",
                "Program.Color.Red, Program.Color.Green, Program.Color.Blue"
            );

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex;

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsNoMissingValuesIfExhaustive2()
    {
        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        [Color.Red] = ""#FF0000"",
        [Color.Green] = ""#008000"",
        [Color.Blue] = ""#0000FF""
    };

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
"
        );
    }

    [TestMethod]
    public async Task ReportsNoMissingValuesIfExhaustive()
    {
        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" },
        { Color.Green, ""#008000"" },
        { Color.Blue, ""#0000FF"" }
    };

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
"
        );
    }

    [TestMethod]
    public async Task NoDiagnosticsIfWeDoNotUseTheAttribute()
    {
        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue };

    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" },
    };

    public static void Main()
    {
        Console.WriteLine(ColorToHex[Color.Green]);
    }
}
"
        );
    }
}
