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
        context.RegisterSyntaxNodeAction(
            AnalyzeDictionaryInitializerField,
            SyntaxKind.FieldDeclaration
        );
        context.RegisterSyntaxNodeAction(
            AnalyzeDictionaryInitializerProperty,
            SyntaxKind.PropertyDeclaration
        );
    }

    private static void AnalyzeDictionaryInitializerField(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        if (
            context
                .SemanticModel.GetSymbolInfo(fieldDeclaration.Declaration.Type)
                .Symbol?.OriginalDefinition.ToString()
            != "System.Collections.Generic.Dictionary<TKey, TValue>"
        )
            return;

        if (
            !fieldDeclaration
                .AttributeLists.SelectMany(x => x.Attributes)
                .Any(attribute => attribute.ToString() == "Exhaustive")
        )
            return;

        if (
            context.SemanticModel.GetTypeInfo(fieldDeclaration.Declaration.Type).Type
            is not INamedTypeSymbol typeSymbol
        )
            return;

        // Extract the key type (TKey in Dictionary<TKey, TValue>)
        var keyType = typeSymbol.TypeArguments[0];
        if (keyType.TypeKind != TypeKind.Enum)
            return; // Only process dictionaries with enum keys

        var enumValues = keyType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .ToList();

        var variable = fieldDeclaration.Declaration.Variables.FirstOrDefault();
        if (variable == null)
            return;

        if (context.SemanticModel.GetDeclaredSymbol(variable) is not IFieldSymbol fieldSymbol)
            return;

        if (
            variable.Initializer == null
            || variable.Initializer.Value is CollectionExpressionSyntax
        )
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                variable.Identifier.GetLocation(),
                fieldSymbol.Name,
                string.Join(", ", enumValues.Select(FormatEnumName))
            );
            context.ReportDiagnostic(diagnostic);
            return;
        }

        if (variable.Initializer.Value is not ImplicitObjectCreationExpressionSyntax objectCreation)
            return;

        // Extract the initializer list (if present)
        var initializer = objectCreation.Initializer;
        if (initializer == null)
            return;

        // Extract explicitly set keys from the initializer
        // { Key, Value } syntax
        var providedKeys = initializer
            .Expressions.OfType<InitializerExpressionSyntax>()
            .SelectMany(expr => expr.Expressions.OfType<MemberAccessExpressionSyntax>())
            .Concat(
                // [Enum.Value] = "Test" syntax
                initializer
                    .Expressions.OfType<AssignmentExpressionSyntax>()
                    .SelectMany(x => ((ImplicitElementAccessSyntax)x.Left).ArgumentList.Arguments)
                    .Select(arg => arg.Expression)
            )
            .Select(expr => context.SemanticModel.GetConstantValue(expr))
            .Where(val => val.HasValue)
            .Select(val => val.Value);

        // Find missing keys
        var missingKeys = enumValues
            .Where(x => !providedKeys.Contains(x.ConstantValue))
            .Select(FormatEnumName)
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

    private static void AnalyzeDictionaryInitializerProperty(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        if (
            context
                .SemanticModel.GetSymbolInfo(propertyDeclaration.Type)
                .Symbol?.OriginalDefinition.ToString()
            != "System.Collections.Generic.Dictionary<TKey, TValue>"
        )
            return;

        if (
            !propertyDeclaration
                .AttributeLists.SelectMany(x => x.Attributes)
                .Any(attribute => attribute.ToString() == "Exhaustive")
        )
            return;

        if (
            context.SemanticModel.GetTypeInfo(propertyDeclaration.Type).Type
            is not INamedTypeSymbol typeSymbol
        )
            return;

        // Extract the key type (TKey in Dictionary<TKey, TValue>)
        var keyType = typeSymbol.TypeArguments[0];
        if (keyType.TypeKind != TypeKind.Enum)
            return; // Only process dictionaries with enum keys

        var enumValues = keyType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f.HasConstantValue)
            .ToList();

        if (
            propertyDeclaration.Initializer == null
            || propertyDeclaration.Initializer.Value is CollectionExpressionSyntax
        )
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                propertyDeclaration.Identifier.GetLocation(),
                propertyDeclaration.Identifier.ValueText,
                string.Join(", ", enumValues.Select(FormatEnumName))
            );
            context.ReportDiagnostic(diagnostic);
            return;
        }

        if (
            propertyDeclaration.Initializer.Value
            is not ImplicitObjectCreationExpressionSyntax objectCreation
        )
            return;

        // Extract the initializer list (if present)
        var initializer = objectCreation.Initializer;
        if (initializer == null)
            return;

        // Extract explicitly set keys from the initializer
        // { Key, Value } syntax
        var providedKeys = initializer
            .Expressions.OfType<InitializerExpressionSyntax>()
            .SelectMany(expr => expr.Expressions.OfType<MemberAccessExpressionSyntax>())
            .Concat(
                // [Enum.Value] = "Test" syntax
                initializer
                    .Expressions.OfType<AssignmentExpressionSyntax>()
                    .SelectMany(x => ((ImplicitElementAccessSyntax)x.Left).ArgumentList.Arguments)
                    .Select(arg => arg.Expression)
            )
            .Select(expr => context.SemanticModel.GetConstantValue(expr))
            .Where(val => val.HasValue)
            .Select(val => val.Value);

        // Find missing keys
        var missingKeys = enumValues
            .Where(x => !providedKeys.Contains(x.ConstantValue))
            .Select(FormatEnumName)
            .ToList();

        if (missingKeys.Count > 0)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                propertyDeclaration.Identifier.GetLocation(),
                propertyDeclaration.Identifier.ValueText,
                string.Join(", ", missingKeys)
            );
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static string FormatEnumName(IFieldSymbol enumValue)
    {
        var parts = enumValue.OriginalDefinition.ToString().Split('.');

        return string.Join(".", parts[parts.Length - 2], parts[parts.Length - 1]);
    }
}
