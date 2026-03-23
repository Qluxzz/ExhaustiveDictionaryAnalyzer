using ExhaustiveDictionary;

namespace DependencyCompileTest
{
    public class ClassB : ClassA
    {
        public enum Size
        {
            Small,
        };

        [Exhaustive]
        public static readonly System.Collections.Generic.Dictionary<Size, string> SizeToString =
            new System.Collections.Generic.Dictionary<Size, string> { { Size.Small, "S" } };
    }
}
