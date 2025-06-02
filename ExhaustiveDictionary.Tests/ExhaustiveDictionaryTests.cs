using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixVerifier<
    ExhaustiveDictionary.EnumDictionaryAnalyzer,
    ExhaustiveDictionary.AddMissingEnumValuesCodeFixProvider,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier
>;

namespace ExhaustiveDictionary.Tests;

[TestClass]
public sealed class ExhaustiveDictionaryTests
{
    [TestMethod]
    public async Task ReportsMissingValuesInDictionaryOnField()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(16, 47, 16, 57)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

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
}
",
            expected
        );
    }

    [TestMethod]
    public async Task ReportsMissingValuesInDictionaryOnProperty()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(13, 38, 13, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

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
    public async Task ReportsAllEnumValuesAsMissingWhenUsingEmptyCollectionExpression()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.ExhaustiveRule)
            .WithSpan(13, 47, 13, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

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
            .WithSpan(13, 47, 13, 57)
            .WithArguments("ColorToHex", "Color.Red, Color.Green, Color.Blue");

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
}
"
        );
    }

    [TestMethod]
    public async Task ReportsDuplicatedEnumValues()
    {
        var expected = Verify
            .Diagnostic(EnumDictionaryAnalyzer.DuplicatedEntryRule)
            .WithSpan(13, 47, 13, 57)
            .WithArguments("ColorToHex", "Color.Red");

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
            .WithSpan(13, 24, 13, 34);

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

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
            .WithSpan(13, 38, 13, 48);

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

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
            .WithSpan(11, 36, 11, 46);

        await Verify.VerifyAnalyzerAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

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
            .WithSpan(13, 38, 13, 48)
            .WithArguments("ColorToHex", "Color.Green, Color.Blue");

        await Verify.VerifyCodeFixAsync(
            @"
using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

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

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ExhaustiveAttribute : Attribute { }

public static class Program
{
    enum Color { Red, Green, Blue, };

    [Exhaustive]
    static Dictionary<Color, string> ColorToHex = new() { { Color.Red, ""#FF0000"" }, { Color.Green, """" }, { Color.Blue, """" } };
}
"
        );
    }
}
