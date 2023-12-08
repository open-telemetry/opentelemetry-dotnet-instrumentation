// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Loading;

internal interface ILifespanManager : IDisposable
{
    /// <summary>
    /// Track an object so that it is not garbage collected.
    /// Additionally, if the objects implements IDisposable,
    /// it will be disposed together with the manager.
    /// </summary>
    /// <param name="instance">Trackable object</param>
    void Track(object instance);
}
