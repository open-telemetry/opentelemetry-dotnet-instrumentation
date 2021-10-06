using System;
using System.Threading.Tasks;
using Datadog.Trace.AppSec;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.Agent
{
    internal interface IApiRequest
    {
        void AddHeader(string name, string value);

        Task<IApiResponse> PostAsync(ArraySegment<byte> traces);

        Task<IApiResponse> PostAsJsonAsync(IEvent events, JsonSerializer serializer);
    }
}
