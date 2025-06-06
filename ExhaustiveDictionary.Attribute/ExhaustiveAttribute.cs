using System;

namespace ExhaustiveDictionary
{
    /// <summary>
    /// Enable Exhaustive checks on Dictionary
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = false,
        Inherited = false
    )]
    public class ExhaustiveAttribute : Attribute { }
}
