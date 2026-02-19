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
        List<DiagnosticResult> expected =
        [
            Verify
                .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
                .WithSpan(11, 38, 11, 48)
                .WithArguments("ColorToHex", "Color.Green, Color.Blue"),
        ];

        List<DiagnosticResult> expectedAfter =
        [
            DiagnosticResult
                .CompilerError("CS0103")
                .WithSpan(11, 100, 11, 104)
                .WithArguments("TODO"),
            DiagnosticResult
                .CompilerError("CS0103")
                .WithSpan(11, 122, 11, 126)
                .WithArguments("TODO"),
        ];

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
    static Dictionary<Color, string> ColorToHex = new() { { Color.Red, ""#FF0000"" }, { Color.Green, TODO }, { Color.Blue, TODO } };
}
",
            expectedAfter
        );
    }

    [TestMethod]
    public async Task AddsMissingEnumValuesUsingSameFormatWhenUsingCodeFix()
    {
        List<DiagnosticResult> expected =
        [
            Verify
                .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
                .WithSpan(11, 38, 11, 48)
                .WithArguments("ColorToHex", "Color.Green, Color.Blue"),
        ];

        List<DiagnosticResult> expectedAfter =
        [
            DiagnosticResult
                .CompilerError("CS0103")
                .WithSpan(11, 100, 11, 104)
                .WithArguments("TODO"),
            DiagnosticResult
                .CompilerError("CS0103")
                .WithSpan(11, 121, 11, 125)
                .WithArguments("TODO"),
        ];

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
    static Dictionary<Color, string> ColorToHex = new() { [Color.Red] = ""#FF0000"", [Color.Green] = TODO, [Color.Blue] = TODO };
}
",
            expectedAfter
        );
    }

    private static async Task TestCodeFix(
        string before,
        List<DiagnosticResult> diagnosticsBefore,
        string after,
        List<DiagnosticResult> diagnosticsAfter
    )
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

        a.TestState.ExpectedDiagnostics.AddRange(diagnosticsBefore);
        a.FixedState.ExpectedDiagnostics.AddRange(diagnosticsAfter);

        await a.RunAsync(CancellationToken.None);
    }
}
