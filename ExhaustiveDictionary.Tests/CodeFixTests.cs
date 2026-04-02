using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

[TestClass]
public sealed class CodeFixTests
{
    [TestMethod]
    public async Task AddsMissingEnumValuesWhenUsingCodeFix()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 38, 11, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await TestCodeFix(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() { { Color.Red, ""#FF0000"" } };
}
",
            expected,
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() { { Color.Red, ""#FF0000"" }, { Color.Green, """" }, { Color.Blue, """" } };
}
"
        );
    }

    [TestMethod]
    public async Task AddsMissingEnumValuesUsingSameFormatWhenUsingCodeFix()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 38, 11, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await TestCodeFix(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() { [Color.Red] = ""#FF0000"" };
}
",
            expected,
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() { [Color.Red] = ""#FF0000"", [Color.Green] = """", [Color.Blue] = """" };
}
"
        );
    }

    [TestMethod]
    public async Task AddsMissingEnumValuesWhenNotInitialized()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestCodeFix(
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
            expected,
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new()
    {
        {
            Color.Red,
            """"
        },
        {
            Color.Green,
            """"
        },
        {
            Color.Blue,
            """"
        }
    };
}
"
        );
    }

    [TestMethod]
    public async Task AddsMissingEnumValuesWhenUsingNewInitializer()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestCodeFix(
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
            expected,
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new()
    {
        {
            Color.Red,
            """"
        },
        {
            Color.Green,
            """"
        },
        {
            Color.Blue,
            """"
        }
    };
}
"
        );
    }

    [TestMethod]
    public async Task AddsMissingEnumValuesForExplicitTypeAndEmptyInitializer()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 55, 11, 65)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestCodeFix(
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
            expected,
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    private static readonly Dictionary<Color, string> ColorToHex = new Dictionary<Color, string>() { { Color.Red, """" }, { Color.Green, """" }, { Color.Blue, """" } };
}
"
        );
    }

    [TestMethod]
    public async Task AddsMissingEnumValuesWhenUsingEmptyCollectionExpression()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

        await TestCodeFix(
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
            expected,
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new()
    {
        {
            Color.Red,
            """"
        },
        {
            Color.Green,
            """"
        },
        {
            Color.Blue,
            """"
        }
    };
}
"
        );
    }

    private static async Task TestCodeFix(string before, DiagnosticResult diagnostic, string after)
    {
        var a = new CSharpCodeFixTest<
            EnumDictionaryAnalyzer,
            AddMissingEnumValuesCodeFixProvider,
            DefaultVerifier
        >
        {
            ReferenceAssemblies = ReferenceAssemblies.Default.AddAssemblies([
                typeof(ExhaustiveAttribute).Assembly.Location.Replace(".dll", string.Empty),
            ]),
            TestCode = before,
            FixedCode = after,
        };

        a.TestState.ExpectedDiagnostics.AddRange(diagnostic);

        await a.RunAsync(CancellationToken.None);
    }
}
