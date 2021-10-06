using System;

namespace Datadog.Trace.Logging
{
    internal interface ILogEnricher
    {
        void Initialize(Tracer tracer);

        IDisposable Register();
    }
}
