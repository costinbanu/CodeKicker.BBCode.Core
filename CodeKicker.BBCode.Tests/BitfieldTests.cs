using RandomTestValues;
using System;
using System.Collections.Generic;
using Xunit;

namespace CodeKicker.BBCode.Core.Tests
{
    public class BitfieldTests
    {
        [Theory]
        [InlineData(3, "EA==")]
        [InlineData(6, "Ag==")]
        [InlineData(0, "gA==")]
        [InlineData(8, "AIA=")]
        public void Bitfield_Set_SingleValues_AreCorrect(int index, string expected)
        {
            var bitfield = new Bitfield();
            bitfield.Set(index);
            Assert.Equal(expected, bitfield.GetBase64());
        }

        [Fact]
        public void Bitfield_Clear_IsCorrect()
        {
            var bitfield = new Bitfield();
            bitfield.Set(3);
            Assert.Equal("EA==", bitfield.GetBase64());
            bitfield.Clear(3);
            bitfield.Set(8);
            Assert.Equal("AIA=", bitfield.GetBase64());
        }

        [Fact]
        public void Bitfield_Set_CombinedValues_AreCorrect()
        {
            var bitfield = new Bitfield();
            bitfield.Set(5);
            bitfield.Set(12);
            bitfield.Set(0);
            bitfield.Set(4);
            Assert.Equal("jAg=", bitfield.GetBase64());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Bitfield_Exists_IsCorrect(bool exists)
        {
            var bitfield = new Bitfield();
            var valueCount = RandomValue.Int(10, 3);
            var values = new List<int>();
            for (var i = 0; i < valueCount; i++)
            {
                var value = RandomValue.Int(32, 0);
                bitfield.Set(value);
                values.Add(value);
            }

            Assert.Equal(exists, bitfield.Exists(exists ? values[RandomValue.Int(valueCount, 0)] : RandomValue.Int(64, 33)));
        }
    }
}
