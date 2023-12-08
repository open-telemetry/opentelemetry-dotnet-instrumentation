// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace DependencyListGenerator;

public class TransientDependency
{
    public TransientDependency(string name, string version)
    {
        Name = name;
        Version = version;
    }

    public string Name { get; private set; }

    public string Version { get; private set; }
}
