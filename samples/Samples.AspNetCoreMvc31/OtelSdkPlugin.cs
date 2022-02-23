using System;
using OpenTelemetry.Trace;
using Samples.AspNetCoreMvc.Controllers;

namespace Samples.AspNetCoreMvc;

public class OtelSdkPlugin
{
    public TracerProviderBuilder ConfigureTracerProvider(TracerProviderBuilder builder)
    {
        var typeName = this.ToString();
        Console.WriteLine($"Hello from {typeName}");
        return builder.AddRedisInstrumentation(RedisController.Connection);
    }
}