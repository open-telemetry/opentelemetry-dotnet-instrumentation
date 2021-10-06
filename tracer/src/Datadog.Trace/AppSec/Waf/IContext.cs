using System;
using System.Collections.Generic;
using System.Text;

namespace Datadog.Trace.AppSec.Waf
{
    internal interface IContext : IDisposable
    {
        IResult Run(IDictionary<string, object> args);
    }
}
