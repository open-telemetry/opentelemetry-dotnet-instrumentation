using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Datadog.Trace
{
    /// <summary>
    /// Class representing 64 or 128 bit TraceID.
    /// </summary>
    public readonly struct TraceId
    {
        private readonly bool _is64Bit;
        private readonly ulong _higher;

        private TraceId(ulong higher, ulong lower)
        {
            _is64Bit = false;
            _higher = higher;
            Lower = lower;
        }

        private TraceId(ulong lower)
        {
            _is64Bit = true;
            _higher = 0;
            Lower = lower;
        }

        /// <summary>
        /// Gets TraceId with zero id.
        /// </summary>
        public static TraceId Zero => new TraceId(higher: 0, lower: 0);

        /// <summary>
        /// Gets lower 64 bits of 128 bit traceID or the whole 64 bit traceID.
        /// </summary>
        public ulong Lower { get; }

        /// <summary>
        /// Creates random 128 bit traceId.
        /// </summary>
        /// <returns>Instance of randomly generated <see cref="TraceId"/>.</returns>
        public static TraceId CreateRandom()
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

        /// <summary>
        /// Creates random 64 bit traceId.
        /// </summary>
        /// <returns>Instance of randomly generated <see cref="TraceId"/>.</returns>
        public static TraceId CreateRandom64Bit()
        {
            var lowerBytes = new byte[8];

            var randomNumberGenerator = RandomNumberGenerator.Current;
            Unsafe.WriteUnaligned(ref lowerBytes[0], randomNumberGenerator.Next());

            var lower = (ulong)BitConverter.ToInt64(lowerBytes, startIndex: 0) & 0x7FFFFFFFFFFFFFFF;

            return new TraceId(lower);
        }

        /// <summary>
        /// Creates traceId from given 16 or 32 sign string representing traceId in hexadecimal format.
        /// </summary>
        /// <param name="id">16 or 32 sign string ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed string.</returns>
        public static TraceId CreateFromString(string id)
        {
            Debug.Assert(id.Length == 16 || id.Length == 32, "ID has to have 16 or 32 signs representing trace ID in hexadecimal format.");

            if (id.Length == 16)
            {
                var lower = (ulong)Convert.ToInt64(id, fromBase: 16) & 0x7FFFFFFFFFFFFFFF;
                return new TraceId(lower);
            }
            else
            {
                var higherAsString = id.Substring(startIndex: 0, length: 16);
                var lowerAsString = id.Substring(startIndex: 16, length: 16);

                var higher = (ulong)Convert.ToInt64(higherAsString, fromBase: 16) & 0x7FFFFFFFFFFFFFFF;
                var lower = (ulong)Convert.ToInt64(lowerAsString, fromBase: 16) & 0x7FFFFFFFFFFFFFFF;

                return new TraceId(higher, lower);
            }
        }

        /// <inheritdoc/>
        public override string ToString() => _is64Bit ? $"{Lower:x16}" : $"{_higher:x16}{Lower:x16}";

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is TraceId))
            {
                return false;
            }

            var traceId = (TraceId)obj;

            return Equals(traceId);
        }

        /// <summary>
        /// Indicates if this and another specified instance of TraceId are equal.
        /// </summary>
        /// <param name="other">Trace id to check equality against.</param>
        /// <returns>True if TraceIds are equal, false otherwise.</returns>
        public bool Equals(TraceId other)
        {
            return _higher == other._higher && Lower == other.Lower && _is64Bit == other._is64Bit;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(_higher, Lower, _is64Bit);
        }
    }
}
