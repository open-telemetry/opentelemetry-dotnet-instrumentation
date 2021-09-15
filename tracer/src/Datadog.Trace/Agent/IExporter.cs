using System;
using System.Threading.Tasks;

namespace Datadog.Trace.Agent
{
    internal interface IExporter
    {
        Task<bool> SendTracesAsync(Span[][] traces);
    }
}
