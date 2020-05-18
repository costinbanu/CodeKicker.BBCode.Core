using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public partial class SyntaxTreeNodeTest
    {
        [Fact]
        public void EqualTreesHaveEqualBBCode()
        {
            var tree1 = BBCodeTestUtil.GetAnyTree();
            var tree2 = BBCodeTestUtil.GetAnyTree();
            var bbCode1 = tree1.ToBBCode();
            var bbCode2 = tree2.ToBBCode();
            Assert.Equal(tree1 == tree2, bbCode1 == bbCode2);
        }

        [Fact]
        public void UnequalTexthasUnequalTrees()
        {
            var tree1 = BBCodeTestUtil.GetAnyTree();
            var tree2 = BBCodeTestUtil.GetAnyTree();
            var text1 = tree1.ToText();
            var text2 = tree2.ToText();
            if (text1 != text2) Assert.True(tree1 != tree2);
        }
    }
}
