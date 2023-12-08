// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ExampleLibrary.FakeClient;

public class DogTrick<T>
{
    public string? Message { get; set; }

    public T? Reward { get; set; }
}

public class DogTrick
{
    public string? Message { get; set; }
}
