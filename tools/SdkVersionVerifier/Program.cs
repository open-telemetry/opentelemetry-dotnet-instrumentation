// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionVerifier;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Invalid arguments. Single argument with repository root is required.");
            return 1;
        }

        var directoryRoot = args[0];

        // Set expected dotnet SDK versions based on sample workflow.
        // This set of versions will be expected to be used consistently
        // in GitHub actions workflows and dockerfiles.
        var expectedVersion = ActionWorkflowVerifier.GetExpectedSdkVersionFromSampleWorkflow(directoryRoot);
        if (expectedVersion is null)
        {
            Console.WriteLine("Unable to extract expected SDK version from sample workflow file.");
            return 1;
        }

        Console.WriteLine($"Expected SDK versions: {expectedVersion}");
        if (!ActionWorkflowVerifier.VerifyVersions(directoryRoot, expectedVersion))
        {
            Console.WriteLine("Invalid SDK versions in GitHub actions workflows.");
            return 1;
        }

        if (!DockerfileVerifier.VerifyVersions(directoryRoot, expectedVersion))
        {
            Console.WriteLine("Invalid SDK versions in dockerfiles.");
            return 1;
        }

        return 0;
    }
}
