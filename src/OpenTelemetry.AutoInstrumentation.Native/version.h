/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#pragma once

#define STRTMP3(V1, V2, V3) #V1 "." #V2 "." #V3
#define STR3(V1, V2, V3) STRTMP3(V1, V2, V3)
#define STRTMP4(V1, V2, V3, V4) #V1 "." #V2 "." #V3 "." #V4
#define STR4(V1, V2, V3, V4) STRTMP4(V1, V2, V3, V4)
#define VERSION_3PARTS STR3(OTEL_AUTO_VERSION_MAJOR, OTEL_AUTO_VERSION_MINOR, OTEL_AUTO_VERSION_PATCH)
#define VERSION_4PARTS STR4(OTEL_AUTO_VERSION_MAJOR, OTEL_AUTO_VERSION_MINOR, OTEL_AUTO_VERSION_PATCH, 0)

const auto PROFILER_VERSION = VERSION_3PARTS;
const auto FILE_VERSION = VERSION_4PARTS;
