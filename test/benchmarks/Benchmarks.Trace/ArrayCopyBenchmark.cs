using System.Text;
using BenchmarkDotNet.Attributes;
using Datadog.Trace.Util;

namespace Benchmarks.Trace
{
    [MemoryDiagnoser]
    public class ArrayCopyBenchmark
    {
        [Benchmark]
        public void CopyPrimitive()
        {
            string sourceMessagePrefix = "Prefix. ";
            string sourceMessageSuffix = ". Suffix.";
            string sourceMessage = string.Concat(sourceMessagePrefix, "this is a test string", sourceMessageSuffix);

            int prefixLength = Encoding.UTF8.GetBytes(sourceMessagePrefix).Length;
            int suffixLength = Encoding.UTF8.GetBytes(sourceMessageSuffix).Length;

            byte[] source = Encoding.UTF8.GetBytes(sourceMessage);
            byte[] target = new byte[source.Length - prefixLength - suffixLength];

            ArrayHelper.Copy(source, target, prefixLength, 0, source.Length - prefixLength - suffixLength);
        }

        [Benchmark]
        public void CopyPrimitiveFast()
        {
            byte[] bytes = new byte[10];
            bytes[0] = 0x99;
            bytes[1] = 0x88;
            bytes[2] = 0x77;
            bytes[3] = 0x66;
            bytes[4] = 0x55;
            bytes[5] = 0x44;
            bytes[6] = 0x33;
            bytes[7] = 0x22;
            bytes[8] = 0x11;
            bytes[9] = 0x00;

            byte[] newArray = ArrayHelper.Copy(bytes, 8, 8);
        }

        [Benchmark]
        public void CopyNonPrimitive()
        {
            string[] strings = new string[10];
            strings[0] = "A9";
            strings[1] = "B8";
            strings[2] = "C7";
            strings[3] = "D6";
            strings[4] = "E5";
            strings[5] = "F4";
            strings[6] = "G3";
            strings[7] = "H2";
            strings[8] = "I1";
            strings[9] = "J0";

            string[] newArray = ArrayHelper.Copy(strings, 8, 8);
        }
    }
}
