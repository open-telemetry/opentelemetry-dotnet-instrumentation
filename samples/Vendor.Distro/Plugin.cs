using System;
using OpenTelemetry.Trace;

namespace Vendor.Distro
{
    public class Plugin
    {
        public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
        {
            var typeName = this.ToString();
            Console.WriteLine($"Hello from {typeName}");
            return builder;
        }
    }
}
