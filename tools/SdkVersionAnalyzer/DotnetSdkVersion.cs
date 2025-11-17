// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionAnalyzer;

internal record DotnetSdkVersion(string? Net8SdkVersion, string? Net9SdkVersion, string? Net10SdkVersion);
