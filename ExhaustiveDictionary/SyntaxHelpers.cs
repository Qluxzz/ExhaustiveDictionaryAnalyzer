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

            if (initializer.Value is ImplicitObjectCreationExpressionSyntax ioc)
                return ioc.Initializer;

            if (initializer.Value is ObjectCreationExpressionSyntax oc)
                return oc.Initializer;

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
