using System;
using System.Collections.Generic;
using System.Text;

namespace Datadog.Trace.AppSec.Waf
{
    internal interface IWaf : IDisposable
    {
        public IContext CreateContext();
    }
}
