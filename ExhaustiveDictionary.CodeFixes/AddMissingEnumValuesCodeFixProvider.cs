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
            EqualsValueClauseSyntax initializer;

            if (node is FieldDeclarationSyntax fieldDeclaration)
            {
                typeSyntax = fieldDeclaration.Declaration.Type;
                initializer = fieldDeclaration.Declaration.Variables.First().Initializer;
            }
            else if (node is PropertyDeclarationSyntax propertyDeclaration)
            {
                typeSyntax = propertyDeclaration.Type;
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

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Case: collection expression (e.g., = []) – replace with new() { ... }
            if (initializer?.Value is CollectionExpressionSyntax)
            {
                var allEntries = CreateInitializerEntries(enumValues, useIndexerStyle: false);
                var newInitList = CreateInlineInitializerExpression(allEntries);
                var newCreation = SyntaxFactory
                    .ImplicitObjectCreationExpression()
                    .WithInitializer(newInitList);
                var equalsClause = SyntaxFactory.EqualsValueClause(newCreation);

                SyntaxNode newDeclaration;
                if (node is FieldDeclarationSyntax fdCe)
                {
                    var varDeclarator = fdCe.Declaration.Variables.First();
                    var newVarDeclarator = varDeclarator.WithInitializer(equalsClause);
                    var newVarDecl = fdCe.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(newVarDeclarator)
                    );
                    newDeclaration = fdCe.WithDeclaration(newVarDecl);
                }
                else
                {
                    var pd = (PropertyDeclarationSyntax)node;
                    newDeclaration = pd.WithInitializer(equalsClause);
                }

                return document.WithSyntaxRoot(root.ReplaceNode(node, newDeclaration));
            }

            // Case: no initializer (e.g., = is missing entirely)
            if (initializer == null)
            {
                var allEntries = CreateInitializerEntries(enumValues, useIndexerStyle: false);
                var newInitList = CreateInlineInitializerExpression(allEntries);
                var newCreation = SyntaxFactory
                    .ImplicitObjectCreationExpression()
                    .WithInitializer(newInitList);
                var equalsClause = SyntaxFactory.EqualsValueClause(newCreation);

                SyntaxNode newDeclaration;
                if (node is FieldDeclarationSyntax fd)
                {
                    var varDeclarator = fd.Declaration.Variables.First();
                    var newVarDeclarator = varDeclarator.WithInitializer(equalsClause);
                    var newVarDecl = fd.Declaration.WithVariables(
                        SyntaxFactory.SingletonSeparatedList(newVarDeclarator)
                    );
                    newDeclaration = fd.WithDeclaration(newVarDecl);
                }
                else
                {
                    var pd = (PropertyDeclarationSyntax)node;
                    newDeclaration = pd.WithInitializer(equalsClause);
                }

                return document.WithSyntaxRoot(root.ReplaceNode(node, newDeclaration));
            }

            // Extract the initializer list (handles ImplicitObjectCreation and ObjectCreation)
            var initList = SyntaxHelpers.ExtractInitializerList(initializer);

            // Case: = new() or new Dictionary<K,V>() with no brace initializer
            if (initList == null)
            {
                var allEntries = CreateInitializerEntries(enumValues, useIndexerStyle: false);
                var newInitList = CreateInlineInitializerExpression(allEntries);

                if (initializer.Value is ImplicitObjectCreationExpressionSyntax iocNoInit)
                {
                    var newCreation = iocNoInit.WithInitializer(newInitList);
                    return document.WithSyntaxRoot(root.ReplaceNode(iocNoInit, newCreation));
                }
                else if (initializer.Value is ObjectCreationExpressionSyntax ocNoInit)
                {
                    var newCreation = ocNoInit.WithInitializer(newInitList);
                    return document.WithSyntaxRoot(root.ReplaceNode(ocNoInit, newCreation));
                }

                return document;
            }

            // Case: existing initializer list – find and add missing entries
            var providedKeys = SyntaxHelpers.GetProvidedKeys(initList, semanticModel);
            var missingValues = enumValues
                .Where(x => !providedKeys.Contains(x.ConstantValue))
                .ToList();

            if (!missingValues.Any())
                return document;

            var useIndexerStyle =
                initList.Expressions.FirstOrDefault() is AssignmentExpressionSyntax;
            var missingEntries = CreateInitializerEntries(missingValues, useIndexerStyle);

            InitializerExpressionSyntax updatedInitList;
            if (!initList.Expressions.Any())
            {
                // Empty ObjectInitializerExpression (e.g. "{ }") must be replaced with the
                // correct CollectionInitializerExpression kind so the syntax is valid.
                updatedInitList = SyntaxFactory.InitializerExpression(
                    SyntaxKind.CollectionInitializerExpression,
                    SyntaxFactory.SeparatedList<ExpressionSyntax>(missingEntries)
                );
            }
            else
            {
                updatedInitList = initList.WithExpressions(
                    initList.Expressions.AddRange(missingEntries)
                );
            }

            return document.WithSyntaxRoot(root.ReplaceNode(initList, updatedInitList));
        }

        private static IEnumerable<ExpressionSyntax> CreateInitializerEntries(
            IEnumerable<IFieldSymbol> fields,
            bool useIndexerStyle
        )
        {
            return fields.Select(field =>
            {
                var keyExpression = MakeMemberAccess(field);
                var valueExpression = MakeEmptyString();

                if (useIndexerStyle)
                {
                    // [Color.Red] = ""
                    return (ExpressionSyntax)
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.ImplicitElementAccess(
                                SyntaxFactory.BracketedArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(keyExpression)
                                    )
                                )
                            ),
                            valueExpression
                        );
                }
                else
                {
                    // { Color.Red, "" }
                    return SyntaxFactory.InitializerExpression(
                        SyntaxKind.ComplexElementInitializerExpression,
                        SyntaxFactory.SeparatedList<ExpressionSyntax>(
                            new SyntaxNodeOrToken[]
                            {
                                keyExpression,
                                SyntaxFactory.Token(SyntaxKind.CommaToken),
                                valueExpression,
                            }
                        )
                    );
                }
            });
        }

        private static InitializerExpressionSyntax CreateInlineInitializerExpression(
            IEnumerable<ExpressionSyntax> entries
        ) =>
            SyntaxFactory.InitializerExpression(
                SyntaxKind.CollectionInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>().AddRange(entries)
            );

        private static MemberAccessExpressionSyntax MakeMemberAccess(IFieldSymbol field) =>
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(field.ContainingType.Name),
                SyntaxFactory.IdentifierName(field.Name)
            );

        private static LiteralExpressionSyntax MakeEmptyString() =>
            SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal("")
            );
    }
}
