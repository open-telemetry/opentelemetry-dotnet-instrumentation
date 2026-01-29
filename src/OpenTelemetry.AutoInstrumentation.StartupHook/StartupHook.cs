// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.RulesEngine;

/// <summary>
/// Dotnet StartupHook
/// </summary>
internal class StartupHook
{
    private const string StartuphookLoggerSuffix = "StartupHook";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger(StartuphookLoggerSuffix);

    // This property must be initialized before any rule is evaluated since it may be used during rule evaluation.
    internal static string? LoaderAssemblyLocation { get; set; }

    /// <summary>
    /// Load and initialize OpenTelemetry.AutoInstrumentation assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        // TODO temporarily trace resolution events at the earliest stage of the application startup to make sure we don't skip resolutions before actual handler setup in Loader
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[StartupHook] Resolving ({args.Name}) from assembly <{args.RequestingAssembly}>: SKIP");
            Console.ResetColor();
            return null;
        };
        System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[StartupHook] Resolving <{assemblyName}>@({context}): SKIP");
            Console.ResetColor();

            return null;
        };
#pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine($"List of Trusted Platfrom Assemblies:");
        var tpaList = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string)?.Split(Path.PathSeparator) ?? [];
        foreach (var it in tpaList)
        {
            Console.WriteLine($" - {it}");
        }
#pragma warning restore CA1303 // Do not pass literals as localized parameters

        _ = bool.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.FailFast), out var failFast);

        try
        {
            LoaderAssemblyLocation = GetLoaderAssemblyLocation();

            var ruleEngine = new RuleEngine();
            if (!ruleEngine.ValidateRules())
            {
                throw new InvalidOperationException(
                    "Rule Engine Failure: One or more rules failed validation. Automatic Instrumentation won't be loaded.");
            }

            Logger.Information("Initialization.");

            // Creating an instance of OpenTelemetry.AutoInstrumentation.Loader.Loader
            // will initialize Instrumentation through its static constructor.
            var loaderFilePath = Path.Combine(LoaderAssemblyLocation, "OpenTelemetry.AutoInstrumentation.Loader.dll");
            var loaderAssembly = Assembly.LoadFrom(loaderFilePath);
            var loaderInstance = loaderAssembly.CreateInstance("OpenTelemetry.AutoInstrumentation.Loader.Loader");
            if (loaderInstance is null)
            {
                if (failFast)
                {
                    throw new InvalidOperationException("StartupHook failed to create an instance of the Loader");
                }
            }
            else
            {
                Logger.Information("StartupHook initialized successfully!");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error in StartupHook initialization: LoaderFolderLocation: {LoaderAssemblyLocation}");
            if (failFast)
            {
                throw;
            }
        }
        finally
        {
            OtelLogging.CloseLogger(StartuphookLoggerSuffix, Logger);
        }
    }

    private static string GetLoaderAssemblyLocation()
    {
        try
        {
            var startupAssemblyFilePath = Assembly.GetExecutingAssembly().Location;
            if (startupAssemblyFilePath.StartsWith(@"\\?\", StringComparison.Ordinal))
            {
                // This will only be used in case the local path exceeds max_path size limit
                startupAssemblyFilePath = startupAssemblyFilePath.Substring(4);
            }

            // StartupHook and Loader assemblies are in the same path
            var startupAssemblyDirectoryPath = Path.GetDirectoryName(startupAssemblyFilePath) ??
                                               throw new InvalidOperationException("StartupAssemblyFilePath is NULL");
            return startupAssemblyDirectoryPath;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting loader directory location: {ex}");
            throw;
        }
    }
}
