using OpenTelemetry.DynamicActivityBinding;
using System;

namespace DemoNetCore31
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DemoNetCore31");

            ActivityStub activity = ActivityFactory.Create();

            Console.WriteLine("Done. Press enter.");
            Console.ReadLine();
        }
    }
}
