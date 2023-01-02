using FsCheck;

namespace CodeKicker.BBCode.Core.Tests
{
    public static class Generators
    {
        public static class NoNewLineString
        {
            public static Arbitrary<string> NoNewLine()
                => Arb.Default.String().Filter(s => s?.Contains('\n') != true);
        }
    }
}
