// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "callbacktest.h"

int32_t ThreadSampling_AdditionalTestMethod(callback fp, int32_t n)
{
    return fp(n);
}

extern "C" int32_t OTelAutoCallbackTest(callback fp, int32_t n)
{
    return ThreadSampling_AdditionalTestMethod(fp, n);
}