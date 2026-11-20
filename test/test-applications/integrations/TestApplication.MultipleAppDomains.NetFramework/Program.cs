// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
#endif

namespace TestApplication.MultipleAppDomains.NetFramework;

using TestApplication.Shared;
using TestLibrary.InstrumentationTarget;

internal static class Program
{
    public static void Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        // Always execute the code to be instrumented.
        var command = new Command();
        command.Execute();

        const string NoAppDomainsSwitch = "--no-app-domains";
#if NETFRAMEWORK
        const string PartialTrustSwitch = "--partial-trust";
        const string ConfiguredAppDomainsSwitch = "--configured-app-domains";
#endif

        if (args?.Length > 0)
        {
            if (args.Length == 1 && ArgumentHelper.HasArgument(args, NoAppDomainsSwitch))
            {
                // Nothing else to do, exit.
                return;
            }

#if NETFRAMEWORK
            if (args.Length == 1 && ArgumentHelper.HasArgument(args, PartialTrustSwitch))
            {
                RunInPartiallyTrustedAppDomain();
                return;
            }

            if (args.Length == 1 && ArgumentHelper.HasArgument(args, ConfiguredAppDomainsSwitch))
            {
                RunConfiguredAppDomains(applicationCodeBase: Assembly.GetExecutingAssembly().CodeBase);
                return;
            }
#endif

            throw new InvalidOperationException($"Unrecognized command-line arguments: \"{string.Join(" ", args)}\"");
        }

#if NETFRAMEWORK
        const int numberOfAppDomains = 4;
        var appDomains = new AppDomain[numberOfAppDomains];
        var tasks = new List<Task>(numberOfAppDomains);
        var applicationCodeBase = Assembly.GetExecutingAssembly().CodeBase;
        for (var i = 0; i < appDomains.Length; i++)
        {
            // Use a name that contains characters that can't be used in file names.
            appDomains[i] = AppDomain.CreateDomain("|invalid:in:file:name|");
            var targetAppDomain = appDomains[i];
            tasks.Add(Task.Run(() => targetAppDomain.ExecuteAssembly(applicationCodeBase, [NoAppDomainsSwitch])));
        }

        Task.WaitAll([.. tasks]);

        for (var i = 0; i < appDomains.Length; i++)
        {
            AppDomain.Unload(appDomains[i]);
        }
#endif
    }

#if NETFRAMEWORK
    [ModuleInitializer]
    internal static void InitializeModule()
    {
        Console.WriteLine("ModuleInitializerRan=True");
    }

    private static void RunInPartiallyTrustedAppDomain()
    {
        var permissions = new PermissionSet(PermissionState.None);
        permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution | SecurityPermissionFlag.UnmanagedCode));
        // DoCallBack serializes the delegate and requires member access.
        permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));

        var appDomain = AppDomain.CreateDomain("PartialTrust", null, AppDomain.CurrentDomain.SetupInformation, permissions);
        try
        {
            appDomain.DoCallBack(ReportPartialTrustLoaderState);
        }
        finally
        {
            AppDomain.Unload(appDomain);
        }
    }

    private static void RunConfiguredAppDomains(string applicationCodeBase)
    {
        for (var i = 1; i <= 2; i++)
        {
            var configFile = Path.GetTempFileName();
            var configuration = $"""
                <configuration><appSettings>
                <add key="OTEL_SERVICE_NAME" value="domain-{i}" />
                <add key="OTEL_RESOURCE_ATTRIBUTES" value="domain.number={i}" />
                <add key="OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES" value="ByteCode.Plugin.StrongNamedValidation" />
                </appSettings></configuration>
                """;
            File.WriteAllText(configFile, configuration);

            try
            {
                var setup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationFile = configFile
                };
                var appDomain = AppDomain.CreateDomain($"configured-domain-{i}", null, setup);
                try
                {
                    appDomain.ExecuteAssembly(applicationCodeBase, ["--no-app-domains"]);
                }
                finally
                {
                    AppDomain.Unload(appDomain);
                }
            }
            finally
            {
                File.Delete(configFile);
            }
        }
    }

    private static void ReportPartialTrustLoaderState()
    {
        Console.WriteLine($"PartialTrustDomainIsFullyTrusted={AppDomain.CurrentDomain.IsFullyTrusted}");
        var loaderLoaded = AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
            assembly.FullName.StartsWith("OpenTelemetry.AutoInstrumentation.Loader,", StringComparison.Ordinal));
        Console.WriteLine($"PartialTrustDomainLoaderLoaded={loaderLoaded}");
    }
#endif
}
