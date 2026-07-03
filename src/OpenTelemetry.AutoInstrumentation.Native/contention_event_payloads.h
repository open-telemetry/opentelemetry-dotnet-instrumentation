// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#pragma once

#include <cstddef>
#include <cstdint>
#include <type_traits>
#include <cstring> // For safe memcpy extraction

namespace continuous_profiler
{

#pragma pack(push, 1)

// ContentionStart, event id 81, version 2.
struct ContentionStartV2Payload
{
    uint8_t   Flags; // 0 = managed contention, 1 = native
    uint16_t  ClrInstanceId;
    uintptr_t LockObjectId;       // managed object address
    uintptr_t AssociatedObjectId; // managed object address
    uint64_t  LockOwnerThreadId;  // Native OS Thread ID (LWP TID on Linux)
};

// ContentionStop, event id 91, version 1.
struct ContentionStopV1Payload
{
    uint8_t  ContentionFlags;
    uint16_t ClrInstanceId;
    double   DurationNs; // CoreCLR emits this as double-precision NANOSECONDS (event 91 field DurationNs)
};

#pragma pack(pop)

// Cross-platform wire-layout assertion. LockObjectId/AssociatedObjectId are pointer-sized
// (nint on the wire), so the size differs by architecture: 27 bytes on 64-bit, 19 on 32-bit.
#if defined(__x86_64__) || defined(__aarch64__) || defined(__LP64__) || defined(_WIN64)
static_assert(sizeof(ContentionStartV2Payload) == 27,
              "ContentionStartV2Payload must match the 27-byte 64-bit wire layout");
#else
static_assert(sizeof(ContentionStartV2Payload) == 19,
              "ContentionStartV2Payload must match the 19-byte 32-bit wire layout");
#endif

static_assert(sizeof(ContentionStopV1Payload) == 11, "ContentionStopV1Payload must match the 11-byte wire layout");

// Safely decodes wire payloads on Linux without triggering undefined behavior
// or unaligned read traps at high compiler optimization levels.
template <typename TPayload>
inline bool TryDecodeEventPayload(const unsigned char* event_data, size_t event_data_size, TPayload& out_payload)
{
    static_assert(std::is_trivially_copyable<TPayload>::value, "Event payload must be trivially copyable");

    if (event_data == nullptr || event_data_size < sizeof(TPayload))
    {
        return false;
    }

    // Compilers optimize this tiny memcpy into direct register assignments (0 overhead)
    std::memcpy(&out_payload, event_data, sizeof(TPayload));
    return true;
}

} // namespace continuous_profiler
