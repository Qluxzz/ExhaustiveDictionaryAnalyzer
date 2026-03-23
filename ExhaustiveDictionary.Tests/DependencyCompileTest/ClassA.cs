using ExhaustiveDictionary;

namespace DependencyCompileTest
{
    public class ClassA
    {
        public enum Color
        {
            Red,
        };

        [Exhaustive]
        public static readonly Dictionary<Color, string> ColorToHex = new Dictionary<Color, string>
        {
            { Color.Red, "#FF0000" },
        };
    }
}
