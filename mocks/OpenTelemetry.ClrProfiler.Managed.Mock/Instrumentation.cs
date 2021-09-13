using System;

namespace OpenTelemetry.ClrProfiler.Managed
{
    public static class Instrumentation
    {
        public static void Initialize()
        {
            Console.WriteLine($"{nameof(Initialize)} called");
        }
    }
}
