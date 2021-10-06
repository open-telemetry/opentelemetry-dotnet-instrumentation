using System;

namespace Datadog.Trace.AppSec.Waf
{
    internal interface IResult : IDisposable
    {
        ReturnCode ReturnCode { get; }

        string Data { get; }
    }
}
