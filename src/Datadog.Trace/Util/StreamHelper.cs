using System;
using System.IO;

namespace Datadog.Trace.Util
{
    internal static class StreamHelper
    {
        /// <summary>
        /// Gets underlyding buffer directly when using supported framework version,
        /// or allocates new buffer if not supported.
        /// </summary>
        /// <param name="byteStream">Stream's buffer to extract</param>
        /// <returns>Underlying or new buffer</returns>
        public static ArraySegment<byte> GetMemoryBuffer(this MemoryStream byteStream)
        {
            ArraySegment<byte> buffer;

#if !NET45
            // GetBuffer returns the underlying storage, which saves an allocation over ToArray.
            if (!byteStream.TryGetBuffer(out buffer))
            {
#endif
            buffer = new ArraySegment<byte>(byteStream.ToArray(), 0, (int)byteStream.Length);
#if !NET45
            }
#endif

            return buffer;
        }
    }
}
