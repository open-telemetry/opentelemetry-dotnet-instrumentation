using System;
using System.Runtime.CompilerServices;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace
{
    internal readonly struct TraceId : ITraceId
    {
        private readonly ulong _higher;

        private TraceId(ulong higher, ulong lower)
        {
            _higher = higher;
            Lower = lower;
        }

        public ulong Lower { get; }

        public override string ToString()
        {
            return $"{_higher:x8}{Lower:x8}";
        }

        internal static TraceId CreateRandom()
        {
            var higherBytes = new byte[8];
            var lowerBytes = new byte[8];

            var randomNumberGenerator = RandomNumberGenerator.Current;
            Unsafe.WriteUnaligned(ref higherBytes[0],  randomNumberGenerator.Next());
            Unsafe.WriteUnaligned(ref lowerBytes[0], randomNumberGenerator.Next());

            var higher = (ulong)BitConverter.ToInt64(higherBytes, startIndex: 0) & 0x7FFFFFFFFFFFFFFF;
            var lower = (ulong)BitConverter.ToInt64(lowerBytes, startIndex: 0) & 0x7FFFFFFFFFFFFFFF;

            return new TraceId(higher, lower);
        }

        internal static TraceId CreateFromString(string id)
        {
            var higherAsString = id.Substring(startIndex: 0, length: 16);
            var lowerAsString = id.Substring(startIndex: 16, length: 16);

            var higher = (ulong)Convert.ToInt64(higherAsString, fromBase: 16) & 0x7FFFFFFFFFFFFFFF;
            var lower = (ulong)Convert.ToInt64(lowerAsString, fromBase: 16) & 0x7FFFFFFFFFFFFFFF;

            return new TraceId(higher, lower);
        }
    }
}
