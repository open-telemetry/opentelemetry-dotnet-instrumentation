using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace IntegrationTests.Helpers;

/// <summary>
/// General use utility methods for all tests and tools.
/// </summary>
public class EnvironmentTools
{
    public const string ProfilerClsId = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";
    public const string DotNetFramework = ".NETFramework";
    public const string CoreFramework = ".NETCoreApp";

    private static string _solutionDirectory = null;

    /// <summary>
    /// Find the solution directory from anywhere in the hierarchy.
    /// </summary>
    /// <returns>The solution directory.</returns>
    public static string GetSolutionDirectory()
    {
        if (_solutionDirectory == null)
        {
            var startDirectory = Environment.CurrentDirectory;
            var currentDirectory = Directory.GetParent(startDirectory);
            const string searchItem = @"OpenTelemetry.AutoInstrumentation.sln";

            while (true)
            {
                var slnFile = currentDirectory.GetFiles(searchItem).SingleOrDefault();

                if (slnFile != null)
                {
                    break;
                }

                currentDirectory = currentDirectory.Parent;

                if (currentDirectory == null || !currentDirectory.Exists)
                {
                    throw new Exception($"Unable to find solution directory from: {startDirectory}");
                }
            }

            _solutionDirectory = currentDirectory.FullName;
        }

        return _solutionDirectory;
    }

    public static string GetOS()
    {
        return IsWindows()                                       ? "win" :
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX)   ? "osx" :
            string.Empty;
    }

    public static bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    public static bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public static bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    public static string GetPlatform()
    {
        return RuntimeInformation.ProcessArchitecture.ToString();
    }

    public static string GetBuildConfiguration()
    {
#if DEBUG
        return "Debug";
#else
            return "Release";
#endif
    }
}
