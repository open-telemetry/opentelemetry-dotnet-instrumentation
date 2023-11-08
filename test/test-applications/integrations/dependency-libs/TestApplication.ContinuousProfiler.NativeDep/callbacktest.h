// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstdint>

typedef int32_t (*callback)(int32_t n);
extern "C" int32_t OTelAutoCallbackTest(callback fp, int32_t n);