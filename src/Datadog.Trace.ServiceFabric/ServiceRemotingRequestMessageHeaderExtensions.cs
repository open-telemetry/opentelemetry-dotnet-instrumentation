using System;
using System.Text;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Datadog.Trace.ServiceFabric
{
    internal static class ServiceRemotingRequestMessageHeaderExtensions
    {
        public static bool TryAddHeader(this IServiceRemotingRequestMessageHeader headers, string headerName, string headerValue, Func<string, byte[]> serializer)
        {
            if (!headers.TryGetHeaderValue(headerName, out _))
            {
                headers.AddHeader(headerName, serializer(headerValue));
                return true;
            }

            return false;
        }

        public static string? TryGetHeaderValueString(this IServiceRemotingRequestMessageHeader headers, string headerName)
        {
            if (headers.TryGetHeaderValue(headerName, out byte[] bytes) && bytes?.Length > 0)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return null;
        }
    }
}
