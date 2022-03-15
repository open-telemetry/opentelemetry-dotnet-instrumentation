using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OldReference;

public static class InstrumentedHttpCall
{
    private static readonly string OperationName = $"{nameof(InstrumentedHttpCall)}.{nameof(GetAsync)}";

    static InstrumentedHttpCall()
    {
        Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>> System.Diagnostics.DiagnosticSource assemblies loaded:");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var loaded = assemblies
            .Where(assembly => assembly.FullName.Contains("System.Diagnostics.DiagnosticSource"))
            .Select(assembly => $">>>>>>>>>>>>>>>>>>>>>>> {assembly.FullName}");
            
        Console.WriteLine(string.Join("\n", loaded));
    }

    public static async Task GetAsync(string url)
    {
        var activity = new Activity(OperationName);
        try
        {
            activity.Start();
            activity.AddTag("foo", "bar");

            using var client = new HttpClient();
            Console.WriteLine($"Calling {url}");
            await client.GetAsync(url);
            Console.WriteLine($"Called {url}");
        }
        finally
        {
            activity.Stop();
        }
    }
}