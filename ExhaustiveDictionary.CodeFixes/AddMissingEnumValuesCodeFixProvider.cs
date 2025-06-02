using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ExhaustiveDictionary
{
    [
        ExportCodeFixProvider(
            LanguageNames.CSharp,
            Name = nameof(AddMissingEnumValuesCodeFixProvider)
        ),
        Shared
    ]
    public class AddMissingEnumValuesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(EnumDictionaryAnalyzer.ExhaustiveRule.Id); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context
                .Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start)
                .Parent.AncestorsAndSelf()
                .Where(x => x is FieldDeclarationSyntax || x is PropertyDeclarationSyntax)
                .First();

            if (declaration is null)
                return;

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add missing values from enum",
                    createChangedDocument: c =>
                        AddMissingEnumValuesAsync(
                            context.Document,
                            declaration,
                            context.CancellationToken
                        ),
                    equivalenceKey: "Add missing values from enum"
                ),
                diagnostic
            );
        }

        private static async Task<Document> AddMissingEnumValuesAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken
        )
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

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

            if (!(semanticModel.GetTypeInfo(typeSyntax).Type is INamedTypeSymbol typeSymbol))
                throw new Exception();

            var enumValues = typeSymbol
                .TypeArguments[0]
                .GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => f.HasConstantValue)
                .ToList();

            if (initializer == null || initializer.Value is CollectionExpressionSyntax)
            {
                return document;
            }

            if (!(initializer.Value is ImplicitObjectCreationExpressionSyntax objectCreation))
                return document;

            var initList = objectCreation.Initializer;
            if (initList == null)
                return document;

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
                .Select(expr => semanticModel.GetConstantValue(expr))
                .Where(val => val.HasValue)
                .Select(val => val.Value)
                .ToList();

            var missingKeys = enumValues
                .Where(x => !providedKeys.Contains(x.ConstantValue))
                .ToList();

            return await AddMissingEnumValues(document, initializer, missingKeys);
        }

        private static async Task<Document> AddMissingEnumValues(
            Document document,
            EqualsValueClauseSyntax equalsValueClause,
            List<IFieldSymbol> missingEnumValues
        )
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            if (semanticModel == null)
                return document;

            if (
                !(
                    equalsValueClause.Value
                    is ImplicitObjectCreationExpressionSyntax implicitCreation
                )
            )
                return document;

            var initializer = implicitCreation.Initializer;
            if (initializer == null)
                return document;

            var newEntries = missingEnumValues.Select(field =>
                SyntaxFactory.InitializerExpression(
                    SyntaxKind.ComplexElementInitializerExpression,
                    SyntaxFactory.SeparatedList<ExpressionSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(field.ContainingType.Name),
                                SyntaxFactory.IdentifierName(field.Name)
                            ),
                            SyntaxFactory.Token(SyntaxKind.CommaToken),
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal("")
                            ),
                        }
                    )
                )
            );

            var updatedInitializer = initializer.WithExpressions(
                initializer.Expressions.AddRange(newEntries)
            );
            var updatedCreation = implicitCreation.WithInitializer(updatedInitializer);
            var updatedEqualsValue = equalsValueClause.WithValue(updatedCreation);

            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            var newRoot = root.ReplaceNode(equalsValueClause, updatedEqualsValue);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
