// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionAnalyzer;

internal sealed record DotnetSdkVersion(string? Net10SdkVersion, string? Net11SdkVersion);
