using System;
using System.Net;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent.Transports
{
    internal class ApiWebRequest : IApiRequest
    {
        private readonly HttpWebRequest _request;

        public ApiWebRequest(HttpWebRequest request)
        {
            _request = request;

            // Default headers
            foreach (var pair in AgentHttpHeaderNames.DefaultHeaders)
            {
                _request.Headers.Add(pair.Key, pair.Value);
            }
        }

        public void AddHeader(string name, string value)
        {
            _request.Headers.Add(name, value);
        }

        public async Task<IApiResponse> PostAsync(ArraySegment<byte> traces)
        {
            _request.Method = "POST";
            _request.ContentType = "application/msgpack";

            using (var requestStream = await _request.GetRequestStreamAsync().ConfigureAwait(false))
            {
                await requestStream.WriteAsync(traces.Array, traces.Offset, traces.Count).ConfigureAwait(false);
            }

            try
            {
                var httpWebResponse = (HttpWebResponse)await _request.GetResponseAsync().ConfigureAwait(false);
                return new ApiWebResponse(httpWebResponse);
            }
            catch (WebException exception)
                when (exception.Status == WebExceptionStatus.ProtocolError && exception.Response != null)
            {
                // If the exception is caused by an error status code, ignore it and let the caller handle the result
                return new ApiWebResponse((HttpWebResponse)exception.Response);
            }
        }
    }
}
