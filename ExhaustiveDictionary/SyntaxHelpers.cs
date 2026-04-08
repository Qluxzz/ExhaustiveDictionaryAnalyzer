using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExhaustiveDictionary
{
    public static class SyntaxHelpers
    {
        public static InitializerExpressionSyntax ExtractInitializerList(
            EqualsValueClauseSyntax initializer
        )
        {
            if (initializer == null)
                return null;

            return ExtractFromExpression(initializer.Value);
        }

        private static InitializerExpressionSyntax ExtractFromExpression(
            ExpressionSyntax expression
        )
        {
            if (expression is ImplicitObjectCreationExpressionSyntax ioc)
                return ioc.Initializer;

            if (expression is ObjectCreationExpressionSyntax oc)
                return oc.Initializer;

            // Unwrap chained method calls like .ToFrozenDictionary() / .ToImmutableDictionary()
            if (
                expression is InvocationExpressionSyntax invocation
                && invocation.Expression is MemberAccessExpressionSyntax memberAccess
            )
                return ExtractFromExpression(memberAccess.Expression);

            return null;
        }

        public static List<object> GetProvidedKeys(
            InitializerExpressionSyntax initList,
            SemanticModel semanticModel
        )
        {
            return initList
                .Expressions.OfType<InitializerExpressionSyntax>()
                .Select(expr =>
                    expr.Expressions.OfType<MemberAccessExpressionSyntax>().FirstOrDefault()
                )
                .Where(expr => expr != null)
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
        }
    }
}
