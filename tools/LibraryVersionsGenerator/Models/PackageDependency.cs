// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace LibraryVersionsGenerator.Models;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class PackageDependency : Attribute
{
    public PackageDependency(string packageName, string variableName)
    {
        PackageName = packageName;
        VariableName = variableName;
    }

    public string PackageName { get; }

    public string VariableName { get; }
}
