using System.Diagnostics;
using System.Net.Http;

namespace Samples.StartupHook
{
    class Program
    {
        private static readonly ActivitySource MyActivitySource = new ActivitySource("Samples.StartupHook", "1.0.0");

        static void Main(string[] args)
        {
            using (var activity = MyActivitySource.StartActivity("SayHello"))
            {
                activity?.SetTag("foo", 1);
                activity?.SetTag("bar", "Hello, World!");
                activity?.SetTag("baz", new int[] { 1, 2, 3 });
            }

            var client = new HttpClient();
            client.GetStringAsync("http://httpstat.us/200").Wait();
        }
    }
}
