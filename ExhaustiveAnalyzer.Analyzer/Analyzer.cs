using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExhaustiveAnalyzer.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnumDictionaryAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "DICT002",
        "Dictionary with [Exhaustive] attribute must define values for all Enum keys",
        "Dictionary '{0}' is missing values: {1}",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeDictionaryInitializer, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeDictionaryInitializer(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        // Check if the field is marked with [Exhaustive]
        var variable = fieldDeclaration.Declaration.Variables.FirstOrDefault();
        if (variable == null)
            return;

        var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
        if (fieldSymbol == null)
            return;

        if (!HasExhaustiveAttribute(fieldSymbol))
            return;

        // Ensure the field has an initializer (i.e., " = new Dictionary<Color, string> {...};")
        if (variable.Initializer == null)
            return;

        if (variable.Initializer.Value is CollectionExpressionSyntax)
        {
            // TODO: report all enum values as missing
            return;
        }

        var objectCreation = variable.Initializer.Value as ImplicitObjectCreationExpressionSyntax;
        if (objectCreation == null)
            return;

        var typeSymbol =
            context.SemanticModel.GetTypeInfo(variable.Initializer.Value).Type as INamedTypeSymbol;
        if (typeSymbol == null)
            return;

        // Ensure it's a Dictionary<TKey, TValue>
        if (
            !typeSymbol
                .OriginalDefinition.ToString()
                .StartsWith("System.Collections.Generic.Dictionary")
        )
            return;

        // Extract the key type (TKey in Dictionary<TKey, TValue>)
        var keyType = typeSymbol.TypeArguments[0];
        if (keyType.TypeKind != TypeKind.Enum)
            return; // Only process dictionaries with enum keys

        // Extract the initializer list (if present)
        var initializer = objectCreation.Initializer;
        if (initializer == null)
            return;

        // Get all defined values of the enum
        var enumValues = keyType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .ToList();

        // Extract explicitly set keys from the initializer
        var providedKeys = initializer
            .Expressions.OfType<InitializerExpressionSyntax>() // Looks for `{ EnumValue, "Value" }`
            .SelectMany(expr => expr.Expressions.OfType<MemberAccessExpressionSyntax>())
            .Select(expr => context.SemanticModel.GetConstantValue(expr))
            .Where(val => val.HasValue)
            .Select(val => val.Value)
            .ToImmutableHashSet();

        // Find missing keys
        var missingKeys = enumValues
            .Where(x => !providedKeys.Contains(x.ConstantValue))
            .Select(x => x.OriginalDefinition)
            .ToList();

        if (missingKeys.Count > 0)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                variable.Identifier.GetLocation(),
                fieldSymbol.Name,
                string.Join(", ", missingKeys)
            );
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasExhaustiveAttribute(IFieldSymbol fieldSymbol)
    {
        // Look for the 'Exhaustive' attribute on the field
        var exhaustiveAttribute = fieldSymbol
            .GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.Name == "ExhaustiveAttribute");
        return exhaustiveAttribute != null;
    }
}
