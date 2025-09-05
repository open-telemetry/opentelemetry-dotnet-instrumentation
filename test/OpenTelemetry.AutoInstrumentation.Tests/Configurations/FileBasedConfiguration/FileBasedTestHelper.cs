// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Core;

namespace OpenTelemetry.AutoInstrumentation.Tests.Configurations.FileBasedConfiguration;

internal static class FileBasedTestHelper
{
    public static void MoveParserToScalar(IParser parser)
    {
        parser.MoveNext(); // StreamStart
        parser.MoveNext(); // DocumentStart
        parser.MoveNext(); // Scalar
    }
}
