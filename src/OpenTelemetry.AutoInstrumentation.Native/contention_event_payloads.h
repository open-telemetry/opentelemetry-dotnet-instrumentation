// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#pragma once

#include <cstddef>
#include <cstdint>
#include <type_traits>

namespace continuous_profiler
{
// Wire layouts for the CLR EventPipe contention events emitted by the
// Microsoft-Windows-DotNETRuntime provider under the CONTENTION_KEYWORD.
//
// Packed to match the on-wire byte layout exactly. The ContentionStart (event 81,
// version 2) layout is EMPIRICALLY VERIFIED on x64: a live probe observed
// cbEventData == 27 with the fields at these offsets, and the static_assert locks
// that invariant.
//
// WARNING - x86 is UNVERIFIED: the object-reference fields (LockObjectId,
// AssociatedObjectId) are pointer-sized, so on a 32-bit runtime the offsets and
// total size differ. Re-probe the raw bytes before trusting contention data on
// x86. The DecodeEventPayload size guard keeps a mismatch memory-safe (it reads
// only within event_data_size) but the decoded field values would be wrong.

#pragma pack(push, 1)

// ContentionStart, event id 81, version 2. Delivered on the contending thread.
// LockOwnerThreadId is an OS thread id (NOT a managed ThreadID) and is best-effort.
struct ContentionStartV2Payload
{
    uint8_t   Flags;              // 0 = managed contention, 1 = native
    uint16_t  ClrInstanceId;
    uintptr_t LockObjectId;       // managed object address (pointer-sized)
    uintptr_t AssociatedObjectId; // managed object address (pointer-sized)
    uint64_t  LockOwnerThreadId;  // OS thread id, stored as a fixed 64-bit field
};

// ContentionStop, event id 91, version 1. Delivered on the thread leaving
// contention. Carries NO lock id - the lock is recovered downstream by
// reverse-resolving the OS thread id through the wait-for graph.
struct ContentionStopV1Payload
{
    uint8_t  ContentionFlags; // 0 = managed contention, 1 = native
    uint16_t ClrInstanceId;
    double   DurationNs;       // duration of the completed contention (no spinning)
};

#pragma pack(pop)

#if defined(HOST_64BIT) || defined(BIT64)
static_assert(sizeof(ContentionStartV2Payload) == 27,
              "ContentionStartV2Payload must match the empirically verified 27-byte x64 wire layout");
#endif
static_assert(sizeof(ContentionStopV1Payload) == 11,
              "ContentionStopV1Payload must match the 11-byte contention-stop wire layout");

// Bounds-checked, zero-copy view of a fixed-size event payload, templatized on the
// static payload type. Returns nullptr if the delivered buffer is too small for
// TPayload, so callers never read out of bounds on a short or unexpected-version
// payload. TPayload must be a packed, trivially copyable wire struct.
template <typename TPayload>
inline const TPayload* DecodeEventPayload(const unsigned char* event_data, size_t event_data_size)
{
    static_assert(std::is_trivially_copyable<TPayload>::value,
                  "Event payload must be trivially copyable to alias the wire buffer");
    if (event_data == nullptr || event_data_size < sizeof(TPayload))
    {
        return nullptr;
    }
    return reinterpret_cast<const TPayload*>(event_data);
}

} // namespace continuous_profiler