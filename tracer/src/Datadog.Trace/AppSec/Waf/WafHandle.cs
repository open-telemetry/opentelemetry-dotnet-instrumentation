using System;
using Datadog.Trace.AppSec.Waf.NativeBindings;
using Datadog.Trace.Vendors.Serilog;

namespace Datadog.Trace.AppSec.Waf
{
    internal class WafHandle : IDisposable
    {
        private IntPtr ruleHandle;
        private bool disposed;

        public WafHandle(IntPtr ruleHandle)
        {
            this.ruleHandle = ruleHandle;
        }

        ~WafHandle()
        {
            Dispose(false);
        }

        public IntPtr Handle
        {
            get { return ruleHandle; }
        }

        public void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            WafNative.Destroy(ruleHandle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
