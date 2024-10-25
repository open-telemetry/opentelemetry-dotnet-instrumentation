// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace SdkVersionVerifier;

internal record DotnetSdkVersion(string? Net6SdkVersion, string? Net7SdkVersion, string? Net8SdkVersion);
