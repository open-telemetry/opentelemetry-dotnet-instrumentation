// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using TestApplication.Shared;

namespace TestApplication.Modules;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;
            ConsoleHelper.WriteSplashScreen(args);
            if (args.Length < 2)
            {
                throw new ArgumentException("Temp path is not provided. Use '--temp-path /my/path/to/temp_file'");
            }

            var contents = JsonConvert.SerializeObject((from x in AppDomain.CurrentDomain.GetAssemblies()
                                                           select x.GetName().Name into name
                                                           where name?.StartsWith("OpenTelemetry") ?? false
                                                           orderby name
                                                           select name).ToList());
            File.WriteAllText(args[1], contents);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception from Main " + e);
            throw;
        }
    }

    private static void UnhandledExceptionEventHandler(object sender, UnhandledExceptionEventArgs args)
    {
        var ex = (Exception)args.ExceptionObject;
        Console.WriteLine("UnhandledExceptionEventHandler caught : " + ex.Message);
        Console.WriteLine("Runtime terminating: {0}", args.IsTerminating);
        Environment.Exit(1);
    }
}
