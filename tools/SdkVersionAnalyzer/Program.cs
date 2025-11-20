// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionAnalyzer;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("At least 2 arguments required - operation mode and directory root.");
            return 1;
        }

        var operationMode = args[0];
        var directoryRoot = args[1];
        switch (operationMode)
        {
            case "--verify" when args.Length == 2:
                {
                    return VerifyDotnetSdkVersions(directoryRoot);
                }

            case "--modify" when args.Length == 4:
                {
                    var requestedSdkVersions = new DotnetSdkVersion(args[2], args[3], args[4]);
                    ModifyDotnetSdkVersions(directoryRoot, requestedSdkVersions);
                    return 0;
                }

            default:
                {
                    Console.WriteLine("Invalid arguments.");
                    return 1;
                }
        }
    }

    private static void ModifyDotnetSdkVersions(string directoryRoot, DotnetSdkVersion requestedSdkVersions)
    {
        ActionWorkflowAnalyzer.ModifyVersions(directoryRoot, requestedSdkVersions);
        DockerfileAnalyzer.ModifyVersions(directoryRoot, requestedSdkVersions);
    }

    private static int VerifyDotnetSdkVersions(string directoryRoot)
    {
        // Set expected dotnet SDK versions based on sample workflow.
        // This set of versions will be expected to be used consistently
        // in GitHub actions workflows and dockerfiles.
        var expectedVersion = ActionWorkflowAnalyzer.GetExpectedSdkVersionFromSampleWorkflow(directoryRoot);
        if (expectedVersion is null)
        {
            Console.WriteLine("Unable to extract expected SDK version from sample workflow file.");
            return 1;
        }

        Console.WriteLine($"Expected SDK versions: {expectedVersion}");
        if (!ActionWorkflowAnalyzer.VerifyVersions(directoryRoot, expectedVersion))
        {
            Console.WriteLine("Invalid SDK versions in GitHub actions workflows.");
            return 1;
        }

        if (!SdkVersionAnalyzer.DockerfileAnalyzer.VerifyVersions(directoryRoot, expectedVersion))
        {
            Console.WriteLine("Invalid SDK versions in dockerfiles.");
            return 1;
        }

        return 0;
    }
}
