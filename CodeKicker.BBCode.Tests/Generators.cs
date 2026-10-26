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
                => ArbMap.Default.ArbFor<string>().Filter(s => s?.Contains('\n') != true);
        }
    }
}
