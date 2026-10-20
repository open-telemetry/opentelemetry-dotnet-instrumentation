// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.LegacySecurityPolicy.NetFramework;

#pragma warning disable CA1515 // Public type is required by AppDomainSetup.
public sealed class CustomAppDomainManager : AppDomainManager;
#pragma warning restore CA1515
