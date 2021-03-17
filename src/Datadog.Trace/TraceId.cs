using System;
using System.Runtime.CompilerServices;

namespace Datadog.Trace
{
    /// <summary>
    /// Class representing 64 or 128 bit TraceID.
    /// </summary>
    public readonly struct TraceId : IEquatable<TraceId>
    {
        private readonly bool _is64Bit;
        private readonly ulong _higher;
        private readonly string _hexString;

        private TraceId(ulong higher, ulong lower)
        {
            _is64Bit = false;
            _higher = higher;
            Lower = lower;
            _hexString = $"{_higher:x16}{Lower:x16}";
            AsBytes = StringToByteArray(_hexString);
        }

        private TraceId(ulong lower)
        {
            _is64Bit = true;
            _higher = 0;
            Lower = lower;
            _hexString = $"{Lower:x16}";
            AsBytes = StringToByteArray(_hexString);
        }

        /// <summary>
        /// Gets TraceId with zero id.
        /// </summary>
        public static TraceId Zero => new(lower: 0);

        /// <summary>
        /// Gets lower 64 bits of 128 bit traceID or the whole 64 bit traceID.
        /// </summary>
        public ulong Lower { get; }

        /// <summary>
        /// Gets byte array representation of the TraceId.
        /// </summary>
        public byte[] AsBytes { get; }

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
            if (id.Length == 16)
            {
                var lower = Convert.ToUInt64(id, fromBase: 16);
                return new TraceId(lower);
            }

            if (id.Length == 32)
            {
                var higherAsString = id.Substring(startIndex: 0, length: 16);
                var lowerAsString = id.Substring(startIndex: 16, length: 16);

                var higher = Convert.ToUInt64(higherAsString, fromBase: 16);
                var lower = Convert.ToUInt64(lowerAsString, fromBase: 16);

                return new TraceId(higher, lower);
            }
            else
            {
                var lower = ulong.Parse(id);
                return new TraceId(lower);
            }
        }

        /// <summary>
        /// Creates 128 bit traceId from given int.
        /// </summary>
        /// <param name="id">Int32 ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed int.</returns>
        public static TraceId CreateFromInt(int id)
        {
            return new((ulong)id);
        }

        /// <summary>
        /// Creates 128 bit traceId from given ulong.
        /// </summary>
        /// <param name="id">Ulong ID to be parsed.</param>
        /// <returns>Instance of <see cref="TraceId"/> representing the same traceId as the passed ulong.</returns>
        public static TraceId CreateFromUlong(ulong id)
        {
            return new(id);
        }

        /// <summary>
        /// Returns hex representation of TraceId as a string.
        /// </summary>
        /// <returns>Hex representation of TraceId as a string.</returns>
        public override string ToString() => _hexString;

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
            return Lower == other.Lower && _higher == other._higher && _is64Bit == other._is64Bit;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(_higher, Lower, _is64Bit);
        }

        private static byte[] StringToByteArray(string hexString)
        {
            var arr = new byte[hexString.Length >> 1];
            for (var i = 0; i < hexString.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hexString[i << 1]) << 4) + GetHexVal(hexString[(i << 1) + 1]));
            }

            return arr;
        }

        private static int GetHexVal(char hex)
        {
            var val = (int)hex;
            // For uppercase A-F letters:
            // return val - (val < 58 ? 48 : 55);
            // For lowercase a-f letters:
            // return val - (val < 58 ? 48 : 87);
            return val - (val < 58
                              ? 48
                              : val < 97
                                  ? 55
                                  : 87);
        }
    }
}
