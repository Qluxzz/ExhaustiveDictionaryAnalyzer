using System;

namespace ExhaustiveDictionary
{
    /// <summary>
    /// Enable Exhaustive checks on Dictionary
    /// </summary>
    /// <remarks>
    /// This type is intentionally <c>internal</c>. The analyzer package ships this
    /// file as source that is compiled into every consuming project, so keeping it
    /// non-public gives each project its own private copy. That prevents CS0436
    /// ("type conflicts with the imported type") when several projects that include
    /// the attribute reference one another, so consumers no longer need to limit the
    /// attribute to a single project via IncludeAssets.
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field,
        AllowMultiple = false,
        Inherited = false
    )]
    internal class ExhaustiveAttribute : Attribute { }
}
