using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

[TestClass]
public sealed partial class CodeFixTests
{
    [TestMethod]
    public async Task AddsMissingEnumValuesUsingElementInitializerSyntax()
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
    public async Task AddsMissingEnumValuesUsingIndexInitializerSyntax()
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

    private static async Task TestCodeFix(string before, DiagnosticResult diagnostic, string after)
    {
        var a = new CSharpCodeFixTest<
            EnumDictionaryAnalyzer,
            AddMissingEnumValuesCodeFixProvider,
            DefaultVerifier
        >
        {
            ReferenceAssemblies = ReferenceAssemblies.Default,
            TestCode = before,
            FixedCode = after,
        };

        // Provide the [Exhaustive] attribute as a source file compiled into the test
        // compilation, mirroring how consumers receive it (see ExhaustiveAttributeSource).
        // The fix never touches this file, so the fixed state must contain it unchanged.
        a.TestState.Sources.Add(("ExhaustiveAttribute.cs", ExhaustiveAttributeSource.Value));
        a.FixedState.Sources.Add(("ExhaustiveAttribute.cs", ExhaustiveAttributeSource.Value));

        a.TestState.ExpectedDiagnostics.AddRange(diagnostic);

        await a.RunAsync(CancellationToken.None);
    }
}
