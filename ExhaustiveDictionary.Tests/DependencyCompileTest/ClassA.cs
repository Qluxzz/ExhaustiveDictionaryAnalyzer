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
        public static readonly System.Collections.Generic.Dictionary<Color, string> ColorToHex =
            new System.Collections.Generic.Dictionary<Color, string> {
                { Color.Red, "#FF0000" }
            };
    }
}
