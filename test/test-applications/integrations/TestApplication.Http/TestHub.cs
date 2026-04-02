// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.SignalR;

namespace TestApplication.Http;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
internal sealed class TestHub : Hub;
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
