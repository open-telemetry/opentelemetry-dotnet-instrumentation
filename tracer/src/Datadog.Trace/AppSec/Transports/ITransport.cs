using System;
using System.Threading.Tasks;
using Datadog.Trace.AppSec.EventModel;
using Datadog.Trace.AppSec.Waf;

namespace Datadog.Trace.AppSec.Transport
{
    internal interface ITransport
    {
        Request Request();

        Response Response(bool blocked);

        void Block();

        IContext GetAdditiveContext();

        void SetAdditiveContext(IContext additiveContext);

        void AddRequestScope(Guid guid);
    }
}
