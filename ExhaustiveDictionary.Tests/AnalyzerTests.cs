using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

[TestClass]
public sealed class AnalyzerTests
{
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
        public async Task MappingBetweenTwoDifferentEnumsWorksAsExpected()
        {
            await TestAnalyzer(
                @"
using System;
using System.Collections.Generic;
using ExhaustiveDictionary;

public static class Program
{
    enum Color { Red, Green, Blue };
    enum Size { Small, Medium, Large };

    [Exhaustive]
    static readonly Dictionary<Color, Size> ColorToSize = new() {
        { Color.Red, Size.Small },
        { Color.Green, Size.Medium },
        { Color.Blue, Size.Large }
    };
}
    "
            );
        }

        [TestMethod]
        public async Task UsingExplicitNameSpaceWorksAsExpected()
        {
            var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(11, 45, 11, 56)
            .WithArguments("ColorToSize", "Color.Green");

            await TestAnalyzer(
                @"
using System;
using System.Collections.Generic;

public static class Program
{
    enum Color { Red, Green, Blue };
    enum Size { Small, Medium, Large };

    [ExhaustiveDictionary.Exhaustive]
    static readonly Dictionary<Color, Size> ColorToSize = new() {
        { Color.Red, Size.Small },
        { Color.Blue, Size.Large }
    };
}
    "
            ,expected);
        }

    private static async Task TestAnalyzer(string code, params DiagnosticResult[] diagnostics)
    {
        var a = new CSharpAnalyzerTest<EnumDictionaryAnalyzer, DefaultVerifier>
        {
            TestCode = code,
            ReferenceAssemblies = ReferenceAssemblies.Default.AddAssemblies(
                [typeof(ExhaustiveAttribute).Assembly.Location.Replace(".dll", string.Empty)]
            ),
        };

        if (diagnostics.Length > 0)
        {
            a.TestState.ExpectedDiagnostics.AddRange(diagnostics);
        }

        await a.RunAsync(CancellationToken.None);
    }
}