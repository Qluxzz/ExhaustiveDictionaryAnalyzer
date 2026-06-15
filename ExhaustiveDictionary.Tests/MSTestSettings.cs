using Microsoft.CodeAnalysis.Text;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace ExhaustiveDictionary.Tests;

internal static class ExhaustiveAttributeSource
{
    // The exact attribute source shipped in the package, embedded as a resource (see the
    // .csproj) so tests compile against the real definition rather than a copy that could
    // drift. It must be injected as source text: once the attribute is internal, the type
    // isn't visible to the in-memory test code through a referenced assembly.
    public static readonly SourceText Value = ReadEmbeddedSource();

    private static SourceText ReadEmbeddedSource()
    {
        using var stream =
            typeof(ExhaustiveAttributeSource).Assembly.GetManifestResourceStream(
                "ExhaustiveAttribute.cs"
            ) ?? throw new InvalidOperationException("Embedded ExhaustiveAttribute.cs not found.");
        return SourceText.From(stream);
    }
}
