using System;

namespace OpenTelemetry.AutoInstrumentation
{
    public static class Instrumentation
    {
        public static void Initialize()
        {
            Console.WriteLine($"{nameof(Initialize)} called");
        }
    }
}
