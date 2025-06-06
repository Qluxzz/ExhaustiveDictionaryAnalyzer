using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

[TestClass]
public sealed class ExhaustiveDictionaryTests
{
    private static async Task TestAnalyzer(string code, params DiagnosticResult[] diagnostics)
    {
        var a = new CSharpAnalyzerTest<EnumDictionaryAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(
                [new PackageIdentity("ExhaustiveDictionary.Attribute", "1.0.0")]
            ),
        };

        if (diagnostics.Length > 0)
        {
            a.TestState.ExpectedDiagnostics.AddRange(diagnostics);
        }

        await a.RunAsync(CancellationToken.None);
    }

    private static async Task TestCodeFix(string before, DiagnosticResult diagnostic, string after)
    {
        var a = new CSharpCodeFixTest<
            EnumDictionaryAnalyzer,
            AddMissingEnumValuesCodeFixProvider,
            DefaultVerifier
        >
        {
            ReferenceAssemblies = ReferenceAssemblies.Default.AddPackages(
                [new PackageIdentity("ExhaustiveDictionary.Attribute", "1.0.0")]
            ),
            TestCode = before,
            FixedCode = after,
        };

        a.TestState.ExpectedDiagnostics.AddRange(diagnostic);

        await a.RunAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task ReportsMissingValuesInDictionaryOnField()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(14, 47, 14, 57)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TestAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive, Test]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" }
    };
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsNothingFromAttributeWithSameNameButWrongAssembly()
    {
        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" }
    };
}
"
        );
    }

    [TestMethod]
    public async Task ReportsMissingValuesInDictionaryOnProperty()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 38, 11, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" }
    };
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsMissingValuesInDictionaryOnPropertyIfUsingFullyQualifiedName()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(10, 38, 10, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [ExhaustiveDictionary.Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" }
    };
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
    public async Task ReportsNoMissingValuesIfExhaustive2()
    {
        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        [Color.Red] = ""#FF0000"",
        [Color.Green] = ""#008000"",
        [Color.Blue] = ""#0000FF""
    };
}
"
        );
    }

    [TestMethod]
    public async Task ReportsNoMissingValuesIfExhaustive()
    {
        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" },
        { Color.Green, ""#008000"" },
        { Color.Blue, ""#0000FF"" }
    };
}
"
        );
    }

    [TestMethod]
    public async Task NoDiagnosticsIfWeDoNotUseTheAttribute()
    {
        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" },
    };
}
"
        );
    }

    [TestMethod]
    public async Task ReportsDuplicatedEnumValues()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.DuplicatedEntryRule)
            .WithSpan(11, 47, 11, 57)
            .WithArguments("ColorToHex", "Color.Red");

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };

    [Exhaustive]
    static readonly Dictionary<Color, string> ColorToHex = new() {
        { Color.Red, ""#FF0000"" },
        { Color.Green, ""#008000"" },
        { Color.Blue, ""#0000FF"" },
        { Color.Red, ""#FF0000"" },
    };
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsErrorIfAttributeUsedOnNotADictionary()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.NotApplicableRule)
            .WithSpan(11, 24, 11, 34);

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static List<Color> ColorToHex = new() {
        Color.Red
    };
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsErrorIfAttributeUsedOnDictionaryWithNoEnumAsKey()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.NotApplicableRule)
            .WithSpan(11, 38, 11, 48);

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<string, Color> ColorToHex = new() {
        { ""Red"", Color.Red }
    };
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsErrorIfAttributeUsedOnDictionaryWithNoEnumAsKey2()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.NotApplicableRule)
            .WithSpan(9, 36, 9, 46);

        await TestAnalyzer(
            @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    [Exhaustive]
    static Dictionary<int, string> ColorToHex = new() {
        { 10, ""hello"" }
    };
}
",
            expected
        );
    }

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
}
