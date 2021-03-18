namespace Datadog.Trace
{
    /// <summary>
    /// Extensions for <see cref="TraceId"/> class.
    /// </summary>
    public static class TraceIdExtensions
    {
        /// <summary>
        /// Transforms TraceId into its byte array representation.
        /// </summary>
        /// <param name="traceId"><see cref="TraceId"/> to be transformed.</param>
        /// <returns>Byte array representation of the given <see cref="TraceId"/></returns>
        public static byte[] AsBytes(this TraceId traceId) => StringToByteArray(traceId.ToString());

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
