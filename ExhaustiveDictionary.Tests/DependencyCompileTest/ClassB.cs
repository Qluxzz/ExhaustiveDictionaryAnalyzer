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
        public static readonly Dictionary<Size, string> SizeToString = new Dictionary<Size, string>
        {
            { Size.Small, "S" },
        };
    }
}
