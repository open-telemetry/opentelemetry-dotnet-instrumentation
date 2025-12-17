/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CONTINUOUS_PROFILER_H_
#define OTEL_CONTINUOUS_PROFILER_H_

#include "continuous_profiler_clr_helpers.h"
#include "stack_capture_strategy.h"

#include <mutex>
#include <cinttypes>
#include <future>
#include <vector>
#include <list>
#include <optional>
#include <utility>
#include <unordered_map>
#include <random>
#include <unordered_set>

#ifdef _WIN32
#define EXPORTTHIS __declspec(dllexport)
#else
#define EXPORTTHIS __attribute__((visibility("default")))
#endif

extern "C"
{
    EXPORTTHIS int32_t ContinuousProfilerReadThreadSamples(int32_t len, unsigned char* buf);
    EXPORTTHIS int32_t ContinuousProfilerReadAllocationSamples(int32_t len, unsigned char* buf);
    EXPORTTHIS int32_t SelectiveSamplerReadThreadSamples(int32_t len, unsigned char* buf);
    // ReSharper disable CppInconsistentNaming
    EXPORTTHIS void ContinuousProfilerSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId);
    EXPORTTHIS void SelectiveSamplingStart(uint64_t traceIdHigh, uint64_t traceIdLow);
    EXPORTTHIS void SelectiveSamplingStop(uint64_t traceIdHigh, uint64_t traceIdLow);
    // ReSharper restore CppInconsistentNaming
}

namespace continuous_profiler
{
struct FunctionIdentifier
{
    mdToken  function_token;
    ModuleID module_id;
    bool     is_valid;

    bool operator==(const FunctionIdentifier& p) const
    {
        return function_token == p.function_token && module_id == p.module_id && is_valid == p.is_valid;
    }
};

struct FunctionIdentifierResolveArgs
{
    FunctionID  function_id;

    FunctionIdentifierResolveArgs() = delete;
    FunctionIdentifierResolveArgs(const FunctionID func_id)
        : function_id(func_id)
    {
    }
    bool operator==(const FunctionIdentifierResolveArgs& p) const
    {
        return function_id == p.function_id;
    }
    bool operator!=(const FunctionIdentifierResolveArgs& p) const
    {
        return !(*this == p);
    }
};

struct trace_context
{
    uint64_t trace_id_high_;
    uint64_t trace_id_low_;

    trace_context(): trace_id_high_(0), trace_id_low_(0)
    {
    }
    trace_context(const uint64_t trace_id_high, const uint64_t trace_id_low)
        : trace_id_high_(trace_id_high)
        , trace_id_low_(trace_id_low)
    {
    }

    bool operator==(const trace_context& p) const
    {
        return trace_id_low_ == p.trace_id_low_ && trace_id_high_ == p.trace_id_high_;
    }
    bool operator!=(const trace_context& p) const
    {
        return !(*this == p);
    }
    [[nodiscard]] bool IsDefault() const;
};

class thread_span_context
{
public:
    trace_context trace_context_;
    uint64_t span_id_;

    thread_span_context() : span_id_(0)
    {
    }
    thread_span_context(uint64_t _traceIdHigh, uint64_t _traceIdLow, uint64_t _spanId) :
        trace_context_(_traceIdHigh, _traceIdLow), span_id_(_spanId)
    {
    }

    bool operator==(const thread_span_context& other) const
    {
        return trace_context_ == other.trace_context_ && span_id_ == other.span_id_;
    }
    bool operator!=(const thread_span_context& other) const
    {
        return !(*this == other);
    }

    [[nodiscard]] bool IsDefault() const;
};
}

template <typename... Args>
std::size_t hash_combine(const Args&... args) {
    std::size_t seed = 0;

    (..., (seed ^= std::hash<Args>{}(args)+0x9e3779b9 + (seed << 6) + (seed >> 2)));
    return seed;
}

template <>
struct std::hash<continuous_profiler::FunctionIdentifier>
{
    std::size_t operator()(const continuous_profiler::FunctionIdentifier& k) const noexcept
    {
        return hash_combine(k.function_token, k.module_id, k.is_valid);
    }
};

template <>
struct std::hash<continuous_profiler::FunctionIdentifierResolveArgs>
{
    std::size_t operator()(const continuous_profiler::FunctionIdentifierResolveArgs& k) const noexcept
    {
        return hash_combine(k.function_id);
    }
};

template <>
struct std::hash<continuous_profiler::trace_context>
{
    std::size_t operator()(const continuous_profiler::trace_context& k) const noexcept
    {
        return hash_combine(k.trace_id_high_, k.trace_id_low_);
    }
};

template <>
struct std::hash<continuous_profiler::thread_span_context>
{
    std::size_t operator()(const continuous_profiler::thread_span_context& k) const noexcept
    {
        return hash_combine(k.trace_context_, k.span_id_);
    }
};

namespace continuous_profiler
{
struct SamplingStatistics
{
    int micros_suspended;
    int num_threads;
    int total_frames;
    int name_cache_misses;
    SamplingStatistics() : micros_suspended(0), num_threads(0), total_frames(0), name_cache_misses(0)
    {
    }
    SamplingStatistics(SamplingStatistics const& other) :
        micros_suspended(other.micros_suspended),
        num_threads(other.num_threads),
        total_frames(other.total_frames),
        name_cache_misses(other.name_cache_misses)
    {
    }
};

class ThreadState
{
public:
    trace::WSTRING thread_name_;
    ThreadState()
    {
    }
    ThreadState(ThreadState const& other) : thread_name_(other.thread_name_)
    {
    }
};

class ThreadSamplesBuffer
{
public:
    std::unordered_map<FunctionIdentifier, int> codes_;
    std::vector<unsigned char>* buffer_;

    explicit ThreadSamplesBuffer(std::vector<unsigned char>* buf);
    ~ThreadSamplesBuffer();
    void StartBatch() const;
    void StartSelectedThreadsBatch() const;
    void EndSelectedThreadsBatch() const;
    void WriteSpanContext(const thread_span_context& span_context) const;
    void StartSample(const ThreadState* state, const thread_span_context& span_context) const;
    void StartSampleForSelectedThread(const ThreadState*         state,
                                      const thread_span_context& span_context) const;
    void MarkSelectedForFrequentSampling(bool value) const;
    void RecordFrame(const FunctionIdentifier& fid, const trace::WSTRING& frame);
    void EndSample() const;
    void EndBatch() const;
    void WriteFinalStats(const SamplingStatistics& stats) const;
    void AllocationSample(uint64_t allocSize, const WCHAR* allocType, size_t allocTypeCharLen, ThreadID id, const ThreadState* state, const thread_span_context& span_context) const;

private:
    void WriteCurrentTimeMillis() const;
    void WriteCodedFrameString(const FunctionIdentifier& fid, const trace::WSTRING& str);
    void WriteShort(int16_t val) const;
    void WriteInt(int32_t val) const;
    void WriteString(const WCHAR* s, size_t len) const;
    void WriteString(const trace::WSTRING& str) const;
    void WriteByte(unsigned char b) const;
    void WriteUInt64(uint64_t val) const;
};

} // namespace continuous_profiler

namespace continuous_profiler
{
class ThreadSpanContextMap
{
public:
    void                                                              Put(ThreadID threadId, const thread_span_context& currentSpanContext);
    std::optional<thread_span_context>                                GetContext(ThreadID threadId);
    void                                                              Remove(const thread_span_context& spanContext);
    void                                                              Remove(ThreadID threadId);
    std::unordered_map<ThreadID, thread_span_context>::const_iterator begin() const;
    std::unordered_map<ThreadID, thread_span_context>::const_iterator end() const;

private:
    std::unordered_map<ThreadID, thread_span_context> thread_span_context_map;
};
template <typename TKey, typename TValue>
class NameCache
{
// ModuleID is volatile but it is unlikely to have exactly same pair of Function Token and ModuleId after changes.
// If fails we should end up we Unknown(unknown) as a result
public:
    explicit NameCache(size_t maximum_size, TValue default_value);
    TValue Get(TKey key);
// if max cache size is exceeded it return value which should be disposed
    TValue Put(TKey key, TValue val);
    void Clear();

private:
    TValue default_value_;
    size_t max_size_;
    std::list<std::pair<TKey, TValue>> list_;
    std::unordered_map<TKey, typename std::list<std::pair<TKey, TValue>>::iterator> map_;
};

class NamingHelper
{
public:
    // These are permanent parts of the helper object
    ICorProfilerInfo7* info7_ = nullptr;

    NamingHelper();
    void ClearFunctionIdentifierCache();
    trace::WSTRING* Lookup(const FunctionIdentifier& function_identifier, SamplingStatistics & stats);
    // TODO: rename
    FunctionIdentifier Lookup(const FunctionID functionId, const COR_PRF_FRAME_INFO frameInfo);

    [[nodiscard]] FunctionIdentifier GetFunctionIdentifier(const FunctionID func_id,
                                                           const COR_PRF_FRAME_INFO frame_info) const;
private:
    NameCache<FunctionIdentifier, trace::WSTRING*> function_name_cache_;
    NameCache<FunctionIdentifierResolveArgs, FunctionIdentifier> function_identifier_cache_;
    void GetFunctionName(FunctionIdentifier function_identifier, trace::WSTRING& result) const;

};

// We can get more AllocationTick events than we reasonably want to push to the cloud; this
// structure/logic helps us rate-control this effect.  More details about the algorithm are in the
// implementation of ShouldSample().
class AllocationSubSampler
{
public:
    AllocationSubSampler(uint32_t targetPerCycle, uint32_t secondsPerCycle);
    bool ShouldSample();
    // internal implementation detail that is public for unit testing purposes
    void AdvanceCycle(std::chrono::milliseconds now);

private:
    uint32_t targetPerCycle;
    uint32_t secondsPerCycle;
    uint32_t seenThisCycle;
    uint32_t sampledThisCycle;
    uint32_t seenLastCycle;
    std::chrono::milliseconds nextCycleStartMillis;
    std::mutex sampleLock;
    std::default_random_engine rand;
};

enum class SamplingType : int32_t { Continuous = 1, SelectedThreads = 2 };

class ContinuousProfiler
{
public:
    std::optional<unsigned int> threadSamplingInterval;
    std::optional<unsigned int> selectedThreadsSamplingInterval;
    std::chrono::time_point<std::chrono::steady_clock> nextOutdatedEntriesScan;
    void                        StartThreadSampling();
    void                        Shutdown();
    bool                        IsShutdownRequested() const;
    static void                 InitSelectiveSamplingBuffer();
    unsigned int                maxMemorySamplesPerMinute;
    void                        StartAllocationSampling(unsigned int maxMemorySamplesPerMinute);
    void                        StopAllocationSampling();
    void                        AllocationTick(ULONG dataLen, LPCBYTE data);
    ICorProfilerInfo12*         info12 = nullptr;
    ICorProfilerInfo7*          info7 = nullptr;
    static void                 ThreadCreated(ThreadID thread_id);
    void                        ThreadDestroyed(ThreadID thread_id);
    void                        ThreadNameChanged(ThreadID thread_id, ULONG cch_name, WCHAR name[]);

    void SetGlobalInfo12(ICorProfilerInfo12* info12);
    void SetGlobalInfo7(ICorProfilerInfo7* cor_profiler_info7);
    void SetStackCaptureStrategy(IStackCaptureStrategy* strategy);
    IStackCaptureStrategy* GetStackCaptureStrategy() const;
    ThreadState* GetCurrentThreadState(ThreadID tid);

    std::unordered_map<ThreadID, ThreadState*> managed_tid_to_state_;
    std::mutex thread_state_lock_;
    NamingHelper helper;
    std::unique_ptr<AllocationSubSampler> allocationSubSampler = nullptr;

    // These cycle every sample and/or are owned externally
    ThreadSamplesBuffer* cur_cpu_writer_ = nullptr;
    SamplingStatistics stats_;
    void AllocateBuffer();
    void PublishBuffer();
    mutable std::mutex      shutdown_mutex_;
    std::condition_variable shutdown_cv_;

private:
    std::atomic_bool             shutdown_requested_{ false };
    std::unique_ptr<std::thread> thread_sampling_thread_;
    EVENTPIPE_SESSION            session_ = 0;
    IStackCaptureStrategy*       stack_capture_strategy_ = nullptr; // Non-owning pointer
};

} // namespace continuous_profiler

void AllocationSamplingAppendToBuffer(int32_t appendLen, unsigned char* appendBuf);

bool ThreadSamplingShouldProduceThreadSample();
void ThreadSamplingRecordProducedThreadSample(std::vector<unsigned char>* buf);
// Can return 0 if none are pending
int32_t ThreadSamplingConsumeOneThreadSample(int32_t len, unsigned char* buf);

#endif // OTEL_CONTINUOUS_PROFILER_H_
