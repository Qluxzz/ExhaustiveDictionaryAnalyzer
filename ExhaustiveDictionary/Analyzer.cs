using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ExhaustiveDictionary
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EnumDictionaryAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor ExhaustiveRule = new DiagnosticDescriptor(
            "EXHAUSTIVEDICT0001",
            "Dictionary with [Exhaustive] attribute must define values for all Enum keys",
            "Dictionary '{0}' need to define values for the following keys: {1}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public static readonly DiagnosticDescriptor DuplicatedEntryRule = new DiagnosticDescriptor(
            "EXHAUSTIVEDICT0002",
            "Dictionary with [Exhaustive] attribute must define values for all Enum keys",
            "Dictionary '{0}' has duplicated values for keys: {1}",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(ExhaustiveRule, DuplicatedEntryRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                AnalyzeDictionaryInitializer,
                SyntaxKind.FieldDeclaration,
                SyntaxKind.PropertyDeclaration
            );
        }

        private static void AnalyzeDictionaryInitializer(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            TypeSyntax typeSyntax;
            SyntaxToken identifier;
            EqualsValueClauseSyntax initializer;

            if (node is FieldDeclarationSyntax fieldDeclaration)
            {
                typeSyntax = fieldDeclaration.Declaration.Type;
                identifier = fieldDeclaration.Declaration.Variables.First().Identifier;
                initializer = fieldDeclaration.Declaration.Variables.First().Initializer;
            }
            else if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                typeSyntax = propertyDeclaration.Type;
                identifier = propertyDeclaration.Identifier;
                initializer = propertyDeclaration.Initializer;
            }
            else
            {
                throw new Exception($"Unhandled node type {node.Kind()}");
            }

            if (
                context
                    .SemanticModel.GetSymbolInfo(typeSyntax)
                    .Symbol?.OriginalDefinition.ToString()
                != "System.Collections.Generic.Dictionary<TKey, TValue>"
            )
                return;

            if (
                !node.ChildNodes()
                    .OfType<AttributeListSyntax>()
                    .SelectMany(x => x.Attributes)
                    .Any(attribute => attribute.ToString() == "Exhaustive")
            )
                return;

            if (
                !(context.SemanticModel.GetTypeInfo(typeSyntax).Type is INamedTypeSymbol typeSymbol)
            )
                return;

            var keyType = typeSymbol.TypeArguments[0];
            if (keyType.TypeKind != TypeKind.Enum)
                return;

            var enumValues = keyType
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .ToList();

            if (initializer == null || initializer.Value is CollectionExpressionSyntax)
            {
                var diagnostic = Diagnostic.Create(
                    ExhaustiveRule,
                    identifier.GetLocation(),
                    identifier.ValueText,
                    string.Join(", ", enumValues.Select(FormatEnumName))
                );
                context.ReportDiagnostic(diagnostic);
                return;
            }

            if (!(initializer.Value is ImplicitObjectCreationExpressionSyntax objectCreation))
                return;

            var initList = objectCreation.Initializer;
            if (initList == null)
                return;

            var providedKeys = initList
                .Expressions.OfType<InitializerExpressionSyntax>()
                .SelectMany(expr => expr.Expressions.OfType<MemberAccessExpressionSyntax>())
                .Concat(
                    initList
                        .Expressions.OfType<AssignmentExpressionSyntax>()
                        .SelectMany(x =>
                            ((ImplicitElementAccessSyntax)x.Left).ArgumentList.Arguments
                        )
                        .Select(arg => arg.Expression)
                )
                .Select(expr => context.SemanticModel.GetConstantValue(expr))
                .Where(val => val.HasValue)
                .Select(val => val.Value)
                .ToList();

            var duplicatedKeys = enumValues
                .Where(x => providedKeys.Count(y => y == x.ConstantValue) > 1)
                .Select(FormatEnumName)
                .ToList();

            if (duplicatedKeys.Any())
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DuplicatedEntryRule,
                        identifier.GetLocation(),
                        identifier.ValueText,
                        string.Join(", ", duplicatedKeys)
                    )
                );
            }

            var missingKeys = enumValues
                .Where(x => !providedKeys.Contains(x.ConstantValue))
                .Select(FormatEnumName)
                .ToList();

            if (missingKeys.Count > 0)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ExhaustiveRule,
                        identifier.GetLocation(),
                        identifier.ValueText,
                        string.Join(", ", missingKeys)
                    )
                );
            }
        }

        private static string FormatEnumName(IFieldSymbol enumValue)
        {
            var parts = enumValue.OriginalDefinition.ToString().Split('.');

            return string.Join(".", parts[parts.Length - 2], parts[parts.Length - 1]);
        }
    }
}
