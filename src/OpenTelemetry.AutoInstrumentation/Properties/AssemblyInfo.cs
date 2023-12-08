// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper")]
[assembly: InternalsVisibleTo("OpenTelemetry.AutoInstrumentation.Bootstrapping.Tests")]
[assembly: InternalsVisibleTo("OpenTelemetry.AutoInstrumentation.Tests")]
[assembly: InternalsVisibleTo("IntegrationTests")]
[assembly: InternalsVisibleTo("TestLibrary.InstrumentationTarget")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("NuGetPackagesTests")]
#endif
