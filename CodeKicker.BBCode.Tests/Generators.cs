using CodeKicker.BBCode.Core.SyntaxTree;
using FsCheck;
using FsCheck.Fluent;

namespace CodeKicker.BBCode.Core.Tests
{
    public static class Generators
    {
        public static class NoNewLineString
        {
            public static Arbitrary<string> NoNewLine()
                => Arb.From(
                Gen.ArrayOf(Gen.Choose(32, 126).Select(i => (char)i))
                   .Select(chars => new string(chars)))
                .Filter(s => !s.Contains('\n'));
        }
    }
}
