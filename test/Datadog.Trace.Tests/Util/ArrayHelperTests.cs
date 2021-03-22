using System;
using System.Text;
using Datadog.Trace.Util;
using Xunit;

namespace Datadog.Trace.Tests.Util
{
    public class ArrayHelperTests
    {
        private const string TestMessage = "this is a test string";

        [Fact]
        public void ArrayHelper_Empty()
        {
            byte[] empty1 = ArrayHelper.Empty<byte>();
            byte[] empty2 = ArrayHelper.Empty<byte>();

            Assert.Same(empty1, empty2);
        }

        [Fact]
        public void ArrayHelper_Copy_SrcArray()
        {
            byte[] source = Encoding.UTF8.GetBytes(TestMessage);
            byte[] target = ArrayHelper.Copy(source);

            string message = Encoding.UTF8.GetString(target);

            Assert.Equal(TestMessage, message);
            Assert.NotSame(source, target);
        }

        [Fact]
        public void ArrayHelper_Copy_SrcArrayToGivenTarget()
        {
            byte[] source = Encoding.UTF8.GetBytes(TestMessage);
            byte[] target = new byte[source.Length];

            ArrayHelper.Copy(source, target);

            string message = Encoding.UTF8.GetString(target);

            Assert.Equal(TestMessage, message);
            Assert.NotSame(source, target);
        }

        [Fact]
        public void ArrayHelper_Copy_SrcArrayToGivenTargetAndLength()
        {
            string sourceMessageAddition = ". Additions.";
            string sourceMessage = TestMessage + sourceMessageAddition;

            int additionLength = Encoding.UTF8.GetBytes(sourceMessageAddition).Length;

            byte[] source = Encoding.UTF8.GetBytes(sourceMessage);
            byte[] target = new byte[source.Length - additionLength];

            ArrayHelper.Copy(source, target, source.Length - additionLength);

            string message = Encoding.UTF8.GetString(target);

            Assert.Equal(TestMessage, message);
            Assert.NotSame(source, target);
        }

        [Fact]
        public void ArrayHelper_Copy_SrcArrayToGivenTargetOffsetLength()
        {
            string sourceMessagePrefix = "Prefix. ";
            string sourceMessageSuffix = ". Suffix.";
            string sourceMessage = string.Concat(sourceMessagePrefix, TestMessage, sourceMessageSuffix);

            int prefixLength = Encoding.UTF8.GetBytes(sourceMessagePrefix).Length;
            int suffixLength = Encoding.UTF8.GetBytes(sourceMessageSuffix).Length;

            byte[] source = Encoding.UTF8.GetBytes(sourceMessage);
            byte[] target = new byte[source.Length - prefixLength - suffixLength];

            ArrayHelper.Copy(source, prefixLength, target, 0, source.Length - prefixLength - suffixLength);

            string message = Encoding.UTF8.GetString(target);

            Assert.Equal(TestMessage, message);
            Assert.NotSame(source, target);
        }

        [Fact]
        public void ArrayHelper_Copy_OtherThanPrimitive()
        {
            string[] strings = new string[3];
            strings[0] = "A1";
            strings[1] = "B2";
            strings[2] = "C3";

            string[] newArray = ArrayHelper.Copy(strings, 2, 2);

            Assert.Equal(2, newArray.Length);
            Assert.Single(newArray, item => item == "A1");
            Assert.Single(newArray, item => item == "B2");
        }

        [Fact]
        public void ArrayHelper_Copy_OtherThanPrimitive_When_NewArrayIsSmaller()
        {
            string[] strings = new string[2];
            strings[0] = "A1";
            strings[1] = "B2";

            Assert.Throws<ArgumentException>(() => ArrayHelper.Copy(strings, 1, 2));
        }

        [Fact]
        public void ArrayHelper_Copy_OtherThanPrimitive_When_NewArrayIsLarger()
        {
            string[] strings = new string[3];
            strings[0] = "A1";
            strings[1] = "B2";
            strings[2] = "C3";

            string[] newArray = ArrayHelper.Copy(strings, 3, 2);

            Assert.Equal(3, newArray.Length);
            Assert.Single(newArray, item => item == "A1");
            Assert.Single(newArray, item => item == "B2");
            Assert.Null(newArray[2]);
        }
    }
}
