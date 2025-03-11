namespace ExhaustiveDictionary.Analyzer;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = false,
    Inherited = false
)]
public class ExhaustiveAttribute : Attribute { }
