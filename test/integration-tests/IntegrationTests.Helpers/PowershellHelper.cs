using System.Diagnostics;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public static class PowershellHelper
{
    public static (string StandardOutput, string ErrorOutput) RunCommand(string psCommand, ITestOutputHelper outputHelper)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = @"powershell.exe";
        startInfo.Arguments = $"& {psCommand}";
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        startInfo.Verb = "runas";

        Process process = Process.Start(startInfo);
        ProcessHelper helper = new ProcessHelper(process);
        process.WaitForExit();

        outputHelper.WriteLine($"PS> {psCommand}");
        outputHelper.WriteResult(helper);

        return (helper.StandardOutput, helper.ErrorOutput);
    }
}
