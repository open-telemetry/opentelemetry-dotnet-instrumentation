// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// We want to use std::min, not the windows.h macro
#define NOMINMAX
#include "continuous_profiler.h"
#include "logger.h"
#include <chrono>
#include <map>
#include <algorithm>
#include <shared_mutex>
#include <unordered_set>
#ifndef _WIN32
#include <pthread.h>
#include <codecvt>
#endif

constexpr auto kMaxStringLength = 512UL;

constexpr auto kMaxCodesPerBuffer = 10 * 1000;

// If you change this, consider ThreadSampler.cs too
constexpr auto kSamplesBufferMaximumSize = 200 * 1024;

constexpr auto kSamplesBufferDefaultSize       = 20 * 1024;
constexpr auto kSelectiveSamplingMaxTraces     = 50;
constexpr auto kSelectiveSamplingMaxAgeMinutes = 15;

// If you change these, change ThreadSampler.cs too
constexpr auto kDefaultSamplePeriod = 10000;
constexpr auto kMinimumSamplePeriod = 1000;

constexpr auto kDefaultMaxAllocsPerMinute = 200;

// FIXME make configurable (hidden)?
// These numbers were chosen to keep total overhead under 1 MB of RAM in typical cases (name lengths being the biggest
// variable)
constexpr auto kMaxFunctionNameCacheSize            = 5000;
constexpr auto kVolatileFunctionIdentifierCacheSize = 2000;

// If you squint you can make out that the original bones of this came from sample code provided by the dotnet project:
// https://github.com/dotnet/samples/blob/2cf486af936261b04a438ea44779cdc26c613f98/core/profiling/stacksampling/src/sampler.cpp
// That stack sampling project is worth reading for a simpler (though higher overhead) take on thread sampling.

/*
  Locking/threading design:
  We have the following shared data structures:
  - A buffer for captured thread samples, used by a single writing thread and a single (managed) reading one
  - A buffer for captured allocation samples, used by any application thread at any time
  - a name cache (data structure for building humnan-readable stack traces), used during both thread and
    allocation sampling
  - a cache of thread id->thread name (set by each thread itself, used during either sample type)
  - a cache of thread span context state (set by any application thread at any time, used during either sample type)

  In general we want to keep locks "adjacent" to just one data structure and usage of them local to one
  modifying/reading method to simplify analysis.  However, there are some special cases.
  Here are the locks in use:
  - cpu_buffer_lock guarding access to the thread samples buffer
  - allocation_buffer_lock guarding access to the buffer for allocation samples
  - name_cache_lock, guarding the data structures used for function/class name lookup
  - thread_state_lock_ guarding the thread name map
  - thread_span_context_lock guarding that data structure
  - (special) a profiling_lock so only one type of profiling (thread stacks or allocation sample) runs at a time

  The special cases worth calling out about locking behavior are:
  - Because the stack sampler pauses the CLR, to avoid deadlock it needs to know that no application thread
    is holding a lock it needs (e.g., thread_span_context_lock) while it is paused.  So, it acquires
    all the necessary locks at once before pausing the CLR.
  - Because the thread sampler pauses the whole CLR and then proceeds to walk every
    thread's stack (using the name cache for each stack entry), it obviously needs the
    name_cache_lock but we don't want to pay the overhead of locking/unlocking it in that tight loop.
    So, it is acquired once before the iteration of threads start, and unlike other locks,
    the methods for this are specifically coded assuming you own
  - AllocationTick(s - possibly on multiple threads) and the thread sampler can happen concurrently,
    so the profiling_lock is used with unique_lock and shared_lock to ensure that any allocation samples
    are fully processed before pausing the CLR for thread samples.
*/

static std::mutex                  cpu_buffer_lock = std::mutex();
static std::vector<unsigned char>* cpu_buffer_a;
static std::vector<unsigned char>* cpu_buffer_b;

static std::mutex                  allocation_buffer_lock = std::mutex();
static std::vector<unsigned char>* allocation_buffer      = new std::vector<unsigned char>();

static std::mutex                 selective_sampling_buffer_lock = std::mutex();
static std::vector<unsigned char> selective_sampling_buffer;

static std::mutex                                thread_span_context_lock;
static continuous_profiler::ThreadSpanContextMap thread_span_context_map;

// Keep timestamps as values to handle stalled spans
static std::mutex                                                        selective_sampling_lock;
static std::unordered_map<continuous_profiler::trace_context, long long> selective_sampling_trace_map;
static std::unordered_set<ThreadID>                                      selective_sampling_thread_buffer;

static std::mutex name_cache_lock = std::mutex();

static std::shared_mutex profiling_lock = std::shared_mutex();

static ICorProfilerInfo12* profiler_info; // After feature sets settle down, perhaps this should be refactored and have
                                          // a single static instance of ThreadSampler

// Dirt-simple back pressure system to save overhead if managed code is not reading fast enough
bool ThreadSamplingShouldProduceThreadSample()
{
    std::lock_guard<std::mutex> guard(cpu_buffer_lock);
    return cpu_buffer_a == nullptr || cpu_buffer_b == nullptr;
}
void ThreadSamplingRecordProducedThreadSample(std::vector<unsigned char>* buf)
{
    std::lock_guard<std::mutex> guard(cpu_buffer_lock);
    if (cpu_buffer_a == nullptr)
    {
        cpu_buffer_a = buf;
    }
    else if (cpu_buffer_b == nullptr)
    {
        cpu_buffer_b = buf;
    }
    else
    {
        trace::Logger::Warn("Unexpected buffer drop in ThreadSampling_RecordProducedThreadSample");
        delete buf; // needs to be dropped now
    }
}
// Can return 0 if none are pending
int32_t ThreadSamplingConsumeOneThreadSample(int32_t len, unsigned char* buf)
{
    if (len <= 0 || buf == nullptr)
    {
        trace::Logger::Warn("Unexpected 0/null buffer to ThreadSampling_ConsumeOneThreadSample");
        return 0;
    }
    std::vector<unsigned char>* to_use = nullptr;
    {
        std::lock_guard<std::mutex> guard(cpu_buffer_lock);
        if (cpu_buffer_a != nullptr)
        {
            to_use       = cpu_buffer_a;
            cpu_buffer_a = nullptr;
        }
        else if (cpu_buffer_b != nullptr)
        {
            to_use       = cpu_buffer_b;
            cpu_buffer_b = nullptr;
        }
    }
    if (to_use == nullptr)
    {
        return 0;
    }
    const size_t to_use_len = static_cast<int>(std::min(to_use->size(), static_cast<size_t>(len)));
    memcpy(buf, to_use->data(), to_use_len);
    delete to_use;
    return static_cast<int32_t>(to_use_len);
}

static void AppendToSelectedThreadsSampleBuffer(int32_t appendLen, unsigned char* appendBuf)
{
    if (appendLen <= 0 || appendBuf == nullptr)
    {
        return;
    }
    std::lock_guard<std::mutex> guard(selective_sampling_buffer_lock);

    if (selective_sampling_buffer.size() + appendLen >= kSamplesBufferMaximumSize)
    {
        trace::Logger::Warn("Discarding samples for selected threads. Buffer is full.");
        return;
    }
    selective_sampling_buffer.insert(selective_sampling_buffer.end(), appendBuf, &appendBuf[appendLen]);
}

bool continuous_profiler::trace_context::IsDefault() const
{
    return trace_id_high_ == 0 && trace_id_low_ == 0;
}

bool continuous_profiler::thread_span_context::IsDefault() const
{
    return trace_context_.IsDefault() && span_id_ == 0;
}

void AllocationSamplingAppendToBuffer(int32_t appendLen, unsigned char* appendBuf)
{
    if (appendLen <= 0 || appendBuf == NULL)
    {
        return;
    }
    std::lock_guard<std::mutex> guard(allocation_buffer_lock);

    if (allocation_buffer->size() + appendLen >= kSamplesBufferMaximumSize)
    {
        trace::Logger::Warn("Discarding captured allocation sample. Allocation buffer is full.");
        return;
    }
    allocation_buffer->insert(allocation_buffer->end(), appendBuf, &appendBuf[appendLen]);
}

// Can return 0
static int32_t AllocationSamplingConsumeAndReplaceBuffer(int32_t len, unsigned char* buf)
{
    if (len <= 0 || buf == nullptr)
    {
        trace::Logger::Warn("Unexpected 0/null buffer to ContinuousProfilerReadAllocationSamples");
        return 0;
    }
    std::vector<unsigned char>* to_use = nullptr;
    {
        std::lock_guard<std::mutex> guard(allocation_buffer_lock);
        to_use            = allocation_buffer;
        allocation_buffer = new std::vector<unsigned char>();
        allocation_buffer->reserve(kSamplesBufferDefaultSize);
    }
    if (to_use == nullptr)
    {
        return 0;
    }
    const size_t to_use_len = static_cast<int>(std::min(to_use->size(), static_cast<size_t>(len)));
    memcpy(buf, to_use->data(), to_use_len);
    delete to_use;
    return static_cast<int32_t>(to_use_len);
}

static int32_t SelectiveSamplingConsumeAndClearBuffer(int32_t len, unsigned char* buf)
{
    if (len <= 0 || buf == nullptr)
    {
        trace::Logger::Warn("Unexpected 0/null buffer to SelectiveSamplingConsumeAndClearBuffer");
        return 0;
    }

    std::lock_guard<std::mutex> guard(selective_sampling_buffer_lock);
    const size_t                to_use_len = std::min(selective_sampling_buffer.size(), static_cast<size_t>(len));

    if (to_use_len == 0)
    {
        return 0;
    }
    memcpy(buf, selective_sampling_buffer.data(), to_use_len);
    selective_sampling_buffer.clear();

    return static_cast<int32_t>(to_use_len);
}

namespace continuous_profiler
{

/*
 * The thread samples buffer format is optimized for single-pass and efficient writing by the native sampling thread
 * (which
 * has paused the CLR)
 *
 * It uses a simple byte-opcode format with fairly standard binary encoding of values.  It is entirely positional but is
 * at least versioned
 * so that mismatched components (native writer and managed reader) will not emit nonsense.
 *
 * ints, shorts, and 64-bit longs are written in big-endian format; strings are written as 2-byte-length-prefixed
 * standard windows utf-16 strings
 *
 * I would write out the "spec" for this format here, but it essentially maps to the code
 * (e.g., 0x01 is StartBatch, which is followed by an int versionNumber and a long captureStartTimeInMillis)
 *
 * The bulk of the data is an (unknown length) array of frame strings, which are represented as coded strings in each
 * buffer.
 * Each used string is given a code (starting at 1) - using an old old inline trick, codes are introduced by writing the
 * code as a
 * negative number followed by the definition of the string (length-prefixed) that maps to that code.  Later uses of the
 * code
 * simply use the 2-byte (positive) code, meaning frequently used strings will take only 2 bytes apiece.  0 is reserved
 * for "end of list"
 * since the number of frames is not known up-front.
 *
 * Each buffer can be parsed/decoded independently; the codes and the LRU NameCache are not related.
 */

// defined op codes
constexpr auto kThreadSamplesStartBatch   = 0x01;
constexpr auto kThreadSamplesStartSample  = 0x02;
constexpr auto kThreadSamplesEndBatch     = 0x06;
constexpr auto kThreadSamplesFinalStats   = 0x07;
constexpr auto kAllocationSample          = 0x08;
constexpr auto kSelectedThreadSample      = 0x09;
constexpr auto kSelectedThreadsStartBatch = 0x0A;
constexpr auto kSelectedThreadsEndBatch   = 0x0B;

constexpr auto kCurrentThreadSamplesBufferVersion = 1;

constexpr FunctionIdentifier DefaultFunctionIdentifier = {0, 0, false};

ThreadSamplesBuffer::ThreadSamplesBuffer(std::vector<unsigned char>* buf) : buffer_(buf) {}
ThreadSamplesBuffer::~ThreadSamplesBuffer()
{
    buffer_ = nullptr; // specifically don't delete as this is done by RecordProduced/ConsumeOneThreadSample
}

#define CHECK_SAMPLES_BUFFER_LENGTH()                                                                                  \
    {                                                                                                                  \
        if (buffer_->size() >= kSamplesBufferMaximumSize)                                                              \
        {                                                                                                              \
            return;                                                                                                    \
        }                                                                                                              \
    }

void ThreadSamplesBuffer::StartBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesStartBatch);
    WriteInt(kCurrentThreadSamplesBufferVersion);
    WriteCurrentTimeMillis();
}

void ThreadSamplesBuffer::StartSelectedThreadsBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kSelectedThreadsStartBatch);
}

void ThreadSamplesBuffer::EndSelectedThreadsBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kSelectedThreadsEndBatch);
}

void ThreadSamplesBuffer::WriteSpanContext(const thread_span_context& span_context) const
{
    WriteUInt64(span_context.trace_context_.trace_id_high_);
    WriteUInt64(span_context.trace_context_.trace_id_low_);
    WriteUInt64(span_context.span_id_);
}

void ThreadSamplesBuffer::StartSample(ThreadID                   id,
                                      const ThreadState*         state,
                                      const thread_span_context& span_context) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesStartSample);
    WriteString(state->thread_name_);
    WriteSpanContext(span_context);
    // Feature possibilities: (managed/native) thread priority, cpu/wait times, etc.
}

void ThreadSamplesBuffer::StartSampleForSelectedThread(const ThreadState*         state,
                                                       const thread_span_context& span_context) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kSelectedThreadSample);
    WriteCurrentTimeMillis();
    WriteString(state->thread_name_);
    WriteSpanContext(span_context);
}

void ThreadSamplesBuffer::MarkSelectedForFrequentSampling(bool value) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(value ? 1 : 0);
}

void ThreadSamplesBuffer::AllocationSample(uint64_t                   allocSize,
                                           const WCHAR*               allocType,
                                           size_t                     allocTypeCharLen,
                                           ThreadID                   id,
                                           const ThreadState*         state,
                                           const thread_span_context& span_context) const

{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kAllocationSample);
    WriteCurrentTimeMillis();
    WriteUInt64(allocSize);
    WriteString(allocType, allocTypeCharLen);
    WriteString(state->thread_name_);
    WriteSpanContext(span_context);
}

void ThreadSamplesBuffer::RecordFrame(const FunctionIdentifier& fid, const trace::WSTRING& frame)
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteCodedFrameString(fid, frame);
}
void ThreadSamplesBuffer::EndSample() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteShort(0);
}
void ThreadSamplesBuffer::EndBatch() const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesEndBatch);
}
void ThreadSamplesBuffer::WriteFinalStats(const SamplingStatistics& stats) const
{
    CHECK_SAMPLES_BUFFER_LENGTH()
    WriteByte(kThreadSamplesFinalStats);
    WriteInt(stats.micros_suspended);
    WriteInt(stats.num_threads);
    WriteInt(stats.total_frames);
    WriteInt(stats.name_cache_misses);
}

void ThreadSamplesBuffer::WriteCodedFrameString(const FunctionIdentifier& fid, const trace::WSTRING& str)
{
    const auto found = codes_.find(fid);
    if (found != codes_.end())
    {
        WriteShort(static_cast<int16_t>(found->second));
    }
    else
    {
        const int code = static_cast<int>(codes_.size()) + 1;
        if (codes_.size() + 1 < kMaxCodesPerBuffer)
        {
            codes_[fid] = code;
        }
        WriteShort(static_cast<int16_t>(-code)); // note negative sign indicating definition of code
        WriteString(str);
    }
}
void ThreadSamplesBuffer::WriteShort(int16_t val) const
{
    buffer_->push_back(((val >> 8) & 0xFF));
    buffer_->push_back(val & 0xFF);
}
void ThreadSamplesBuffer::WriteInt(int32_t val) const
{
    buffer_->push_back(((val >> 24) & 0xFF));
    buffer_->push_back(((val >> 16) & 0xFF));
    buffer_->push_back(((val >> 8) & 0xFF));
    buffer_->push_back(val & 0xFF);
}

void ThreadSamplesBuffer::WriteString(const WCHAR* s, size_t charLen) const
{
    // limit strings to a max length overall; this prevents (e.g.) thread names or
    // any other miscellaneous strings that come along from blowing things out
    const short used_len = static_cast<short>(std::min(charLen, static_cast<size_t>(kMaxStringLength)));
    WriteShort(used_len);
    // odd bit of casting since we're copying bytes, not wchars
    const auto str_begin = reinterpret_cast<const unsigned char*>(s);
    // possible endian-ness assumption here; unclear how the managed layer would decode on big endian platforms
    buffer_->insert(buffer_->end(), str_begin, str_begin + used_len * 2);
}

void ThreadSamplesBuffer::WriteString(const trace::WSTRING& str) const
{
    WriteString(str.c_str(), str.length());
}
void ThreadSamplesBuffer::WriteByte(unsigned char b) const
{
    buffer_->push_back(b);
}
void ThreadSamplesBuffer::WriteUInt64(uint64_t val) const
{
    buffer_->push_back(((val >> 56) & 0xFF));
    buffer_->push_back(((val >> 48) & 0xFF));
    buffer_->push_back(((val >> 40) & 0xFF));
    buffer_->push_back(((val >> 32) & 0xFF));
    buffer_->push_back(((val >> 24) & 0xFF));
    buffer_->push_back(((val >> 16) & 0xFF));
    buffer_->push_back(((val >> 8) & 0xFF));
    buffer_->push_back(val & 0xFF);
}

void ThreadSamplesBuffer::WriteCurrentTimeMillis() const
{
    const auto ms =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    WriteUInt64(ms.count());
}

void ThreadSpanContextMap::Remove(const thread_span_context& spanContext)
{
    const auto foundBySpanContext = span_context_thread_map.find(spanContext);
    if (foundBySpanContext == span_context_thread_map.end())
    {
        return; // nothing to remove
    }
    const auto& threadIds = foundBySpanContext->second;
    for (auto threadId : threadIds)
    {
        thread_span_context_map.erase(threadId);
    }
    span_context_thread_map.erase(spanContext);

    const auto foundByTraceContext = trace_active_span_map.find(spanContext.trace_context_);
    if (foundByTraceContext == trace_active_span_map.end())
    {
        return; // nothing to remove
    }
    auto& traceActiveSpans = foundByTraceContext->second;
    traceActiveSpans.erase(spanContext);
    if (traceActiveSpans.empty())
    {
        trace_active_span_map.erase(spanContext.trace_context_);
    }
}

void ThreadSpanContextMap::Remove(ThreadID threadId)
{
    const auto foundByThreadId = thread_span_context_map.find(threadId);
    if (foundByThreadId == thread_span_context_map.end())
    {
        return; // nothing to remove
    }

    const auto spanContext        = foundByThreadId->second;
    const auto foundBySpanContext = span_context_thread_map.find(spanContext);
    if (foundBySpanContext != span_context_thread_map.end())
    {
        auto& threadIds = foundBySpanContext->second;
        threadIds.erase(threadId);
    }

    thread_span_context_map.erase(threadId);
}

void ThreadSpanContextMap::Put(ThreadID threadId, const thread_span_context& currentSpanContext)
{
    const auto foundByThreadId = thread_span_context_map.find(threadId);
    if (foundByThreadId != thread_span_context_map.end())
    {
        const auto previousContext = foundByThreadId->second;
        if (!previousContext.IsDefault())
        {
            span_context_thread_map[previousContext].erase(threadId);
        }
    }

    thread_span_context_map[threadId] = currentSpanContext;
    if (currentSpanContext.IsDefault())
    {
        return;
    }

    span_context_thread_map[currentSpanContext].insert(threadId);

    // Also note of possibly new activity, store it in trace map.
    // This is a noop if already present.
    trace_active_span_map[currentSpanContext.trace_context_].insert(currentSpanContext);
}

void ThreadSpanContextMap::GetAllThreads(const trace_context traceContext, std::unordered_set<ThreadID>& buffer)
{
    const auto traceSpans = trace_active_span_map.find(traceContext);
    if (traceSpans == trace_active_span_map.end())
    {
        return;
    }

    const auto& spanContexts = traceSpans->second;
    for (const auto& spanContext : spanContexts)
    {
        const auto foundBySpanContext = span_context_thread_map.find(spanContext);
        if (foundBySpanContext == span_context_thread_map.end())
        {
            continue;
        }

        const auto& threadIds = foundBySpanContext->second;
        buffer.insert(threadIds.begin(), threadIds.end());
    }
}

std::optional<thread_span_context> ThreadSpanContextMap::GetContext(ThreadID threadId)
{
    const auto iterator = thread_span_context_map.find(threadId);
    if (iterator == thread_span_context_map.end())
    {
        return std::nullopt;
    }
    return iterator->second;
}

NamingHelper::NamingHelper()
    : function_name_cache_(kMaxFunctionNameCacheSize, nullptr)
    , function_identifier_cache_(kVolatileFunctionIdentifierCacheSize, DefaultFunctionIdentifier)
{
}

void ContinuousProfiler::AllocateBuffer()
{
    auto bytes = new std::vector<unsigned char>();
    bytes->reserve(kSamplesBufferDefaultSize);
    cur_cpu_writer_ = new ThreadSamplesBuffer(bytes);
}

void ContinuousProfiler::PublishBuffer()
{
    ThreadSamplingRecordProducedThreadSample(cur_cpu_writer_->buffer_);
    delete cur_cpu_writer_;
    cur_cpu_writer_ = nullptr;
    stats_          = SamplingStatistics();
}

void NamingHelper::ClearFunctionIdentifierCache()
{
    function_identifier_cache_.Clear();
}

[[nodiscard]] FunctionIdentifier NamingHelper::GetFunctionIdentifier(const FunctionID         func_id,
                                                                     const COR_PRF_FRAME_INFO frame_info) const
{
    if (func_id == 0)
    {
        constexpr auto zero_valid_function_identifier = FunctionIdentifier{0, 0, true};
        return zero_valid_function_identifier;
    }

    ModuleID module_id      = 0;
    mdToken  function_token = 0;
    // theoretically there is a possibility to use GetFunctionInfo method, but it does not support generic methods
    const HRESULT hr =
        info12_->GetFunctionInfo2(func_id, frame_info, nullptr, &module_id, &function_token, 0, nullptr, nullptr);
    if (FAILED(hr))
    {
        trace::Logger::Debug("GetFunctionInfo2 failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        constexpr auto zero_invalid_function_identifier = FunctionIdentifier{0, 0, false};
        return zero_invalid_function_identifier;
    }

    return FunctionIdentifier{function_token, module_id, true};
}

void NamingHelper::GetFunctionName(FunctionIdentifier function_identifier, trace::WSTRING& result) const
{
    constexpr auto unknown_list_of_arguments = WStr("(unknown)");
    constexpr auto unknown_function_name     = WStr("Unknown(unknown)");

    if (!function_identifier.is_valid)
    {
        result.append(unknown_function_name);
        return;
    }

    if (function_identifier.function_token == 0)
    {
        constexpr auto unknown_native_function_name = WStr("Unknown_Native_Function(unknown)");
        result.append(unknown_native_function_name);
        return;
    }

    ComPtr<IMetaDataImport2> metadata_import;
    HRESULT hr = info12_->GetModuleMetaData(function_identifier.module_id, ofRead, IID_IMetaDataImport2,
                                            reinterpret_cast<IUnknown**>(&metadata_import));
    if (FAILED(hr))
    {
        trace::Logger::Debug("GetModuleMetaData failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        result.append(unknown_function_name);
        return;
    }

    const auto function_info = GetFunctionInfo(metadata_import, function_identifier.function_token);

    if (!function_info.IsValid())
    {
        trace::Logger::Debug("GetFunctionInfo failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        result.append(unknown_function_name);
        return;
    }

    result.append(function_info.type.name);
    result.append(name_separator);
    result.append(function_info.name);

    HCORENUM       function_gen_params_enum = nullptr;
    HCORENUM       class_gen_params_enum    = nullptr;
    mdGenericParam function_generic_params[kGenericParamsMaxLen]{};
    mdGenericParam class_generic_params[kGenericParamsMaxLen]{};
    ULONG          function_gen_params_count = 0;
    ULONG          class_gen_params_count    = 0;

    mdTypeDef class_token = function_info.type.id;

    hr = metadata_import->EnumGenericParams(&class_gen_params_enum, class_token, class_generic_params,
                                            kGenericParamsMaxLen, &class_gen_params_count);
    metadata_import->CloseEnum(class_gen_params_enum);
    if (FAILED(hr))
    {
        trace::Logger::Debug("Class generic parameters enumeration failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                             std::hex, hr);
        result.append(unknown_list_of_arguments);
        return;
    }

    hr = metadata_import->EnumGenericParams(&function_gen_params_enum, function_identifier.function_token,
                                            function_generic_params, kGenericParamsMaxLen, &function_gen_params_count);
    metadata_import->CloseEnum(function_gen_params_enum);
    if (FAILED(hr))
    {
        trace::Logger::Debug("Method generic parameters enumeration failed. HRESULT=0x", std::setfill('0'),
                             std::setw(8), std::hex, hr);
        result.append(unknown_list_of_arguments);
        return;
    }

    if (function_gen_params_count > 0)
    {
        result.append(kGenericParamsOpeningBrace);
        for (ULONG i = 0; i < function_gen_params_count; ++i)
        {
            if (i != 0)
            {
                result.append(kParamsSeparator);
            }

            WCHAR param_type_name[kParamNameMaxLen]{};
            ULONG pch_name = 0;
            hr = metadata_import->GetGenericParamProps(function_generic_params[i], nullptr, nullptr, nullptr, nullptr,
                                                       param_type_name, kParamNameMaxLen, &pch_name);
            if (FAILED(hr))
            {
                trace::Logger::Debug("GetGenericParamProps failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                                     std::hex, hr);
                result.append(kUnknown);
            }
            else
            {
                result.append(param_type_name);
            }
        }
        result.append(kGenericParamsClosingBrace);
    }

    // try to list arguments type
    FunctionMethodSignature function_method_signature = function_info.method_signature;
    hr                                                = function_method_signature.TryParse();
    if (FAILED(hr))
    {
        result.append(unknown_list_of_arguments);
        trace::Logger::Debug("FunctionMethodSignature parsing failed. HRESULT=0x", std::setfill('0'), std::setw(8),
                             std::hex, hr);
    }
    else
    {
        const auto& arguments = function_method_signature.GetMethodArguments();
        result.append(kFunctionParamsOpeningBrace);
        for (ULONG i = 0; i < arguments.size(); i++)
        {
            if (i != 0)
            {
                result.append(kParamsSeparator);
            }

            result.append(arguments[i].GetTypeTokName(metadata_import, class_generic_params, function_generic_params));
        }
        result.append(kFunctionParamsClosingBrace);
    }
}

trace::WSTRING* NamingHelper::Lookup(const FunctionIdentifier& function_identifier, SamplingStatistics& stats)
{
    trace::WSTRING* answer = function_name_cache_.Get(function_identifier);
    if (answer != nullptr)
    {
        return answer;
    }
    stats.name_cache_misses++;
    answer = new trace::WSTRING();
    this->GetFunctionName(function_identifier, *answer);

    const auto old_value = function_name_cache_.Put(function_identifier, answer);
    delete old_value;

    return answer;
}

FunctionIdentifier NamingHelper::Lookup(const FunctionID functionId, const COR_PRF_FRAME_INFO frameInfo)
{
    const FunctionIdentifierResolveArgs cacheKey         = {functionId, frameInfo};
    const auto                          cachedIdentifier = function_identifier_cache_.Get(cacheKey);
    if (cachedIdentifier.is_valid)
    {
        return cachedIdentifier;
    }
    const auto resolvedIdentifier = this->GetFunctionIdentifier(cacheKey.function_id, cacheKey.frame_info);
    function_identifier_cache_.Put(cacheKey, resolvedIdentifier);
    return resolvedIdentifier;
}

// This is slightly messy since we an only pass one parameter to the FrameCallback
// but we have some slightly different use cases (but want to use the same stack capture
// code for allocations and paused thread samples)
struct DoStackSnapshotParams
{
    ContinuousProfiler*              prof;
    std::vector<FunctionIdentifier>* buffer;
    DoStackSnapshotParams(ContinuousProfiler* p, std::vector<FunctionIdentifier>* b) : prof(p), buffer(b) {}
};

static HRESULT __stdcall FrameCallback(_In_ FunctionID         func_id,
                                       _In_ UINT_PTR           ip,
                                       _In_ COR_PRF_FRAME_INFO frame_info,
                                       _In_ ULONG32            context_size,
                                       _In_ BYTE               context[],
                                       _In_ void*              client_data)
{

    const auto params = static_cast<DoStackSnapshotParams*>(client_data);
    params->prof->stats_.total_frames++;

    const auto identifier = params->prof->helper.Lookup(func_id, frame_info);
    params->buffer->push_back(identifier);

    return S_OK;
}

[[nodiscard]] static SamplingType GetNextSamplingType(const ContinuousProfiler* prof, const unsigned int iteration)
{
    if (!prof->selectedThreadsSamplingInterval.has_value())
    {
        return SamplingType::Continuous;
    }

    if (!prof->threadSamplingInterval.has_value())
    {
        return SamplingType::SelectedThreads;
    }

    // If both enabled, depends on iteration.
    // Every nth iteration is a sampling of all the threads.
    // N is a ratio of standard (continuos) sampling interval and selective sampling interval.
    // Selective sampling is expected to be much more frequent, e.g. every 20ms compared to 10s for continuous
    // profiling.
    const unsigned ratio = prof->threadSamplingInterval.value() / prof->selectedThreadsSamplingInterval.value();
    if (iteration != ratio)
    {
        return SamplingType::SelectedThreads;
    }

    return SamplingType::Continuous;
}

static void CaptureFunctionIdentifiersForThreads(
    ContinuousProfiler*                                            prof,
    ICorProfilerInfo12*                                            info12,
    const std::unordered_set<ThreadID>&                            selectedThreads,
    std::unordered_map<ThreadID, std::vector<FunctionIdentifier>>& threadStacksBuffer)
{
    prof->helper.ClearFunctionIdentifierCache();
    for (auto threadId : selectedThreads)
    {
        DoStackSnapshotParams doStackSnapshotParams(prof, &threadStacksBuffer[threadId]);
        HRESULT               snapshotHr = info12->DoStackSnapshot(threadId, &FrameCallback, COR_PRF_SNAPSHOT_DEFAULT,
                                                                   &doStackSnapshotParams, nullptr, 0);
        if (FAILED(snapshotHr))
        {
            trace::Logger::Debug("DoStackSnapshot failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex,
                                 snapshotHr);
        }
    }
}

static std::unordered_set<ThreadID> EnumerateThreads(ICorProfilerInfo12* info12)
{
    std::unordered_set<ThreadID> threads;

    ICorProfilerThreadEnum* thread_enum = nullptr;
    HRESULT                 hr          = info12->EnumThreads(&thread_enum);
    if (FAILED(hr))
    {
        trace::Logger::Debug("Could not EnumThreads. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        return threads;
    }

    ULONG    num_returned = 0;
    ThreadID thread_id;
    while ((hr = thread_enum->Next(1, &thread_id, &num_returned)) == S_OK)
    {
        threads.insert(thread_id);
    }
    return threads;
}

static void ResolveFrames(ContinuousProfiler*                    prof,
                          const std::vector<FunctionIdentifier>& threadStack,
                          ThreadSamplesBuffer&                   buffer)
{
    for (auto functionIdentifier : threadStack)
    {
        const trace::WSTRING* name = prof->helper.Lookup(functionIdentifier, prof->stats_);
        // This is where line numbers could be calculated
        buffer.RecordFrame(functionIdentifier, *name);
    }
}

static ThreadState* GetThreadState(const std::unordered_map<ThreadID, ThreadState*>& managed_tid_to_state_,
                                   ThreadID                                          thread_id)
{
    static ThreadState UnknownThreadState;
    auto               found = managed_tid_to_state_.find(thread_id);

    const bool threadStateValid = found != managed_tid_to_state_.end() && found->second != nullptr;

    return threadStateValid ? found->second : &UnknownThreadState;
}

static thread_span_context GetContext(ThreadID threadId)
{
    auto retrievedContext = thread_span_context_map.GetContext(threadId);
    return retrievedContext.has_value() ? retrievedContext.value() : thread_span_context{};
}

static void ResolveSymbolsAndPublishBufferForAllThreads(
    ContinuousProfiler* prof, const std::unordered_map<ThreadID, std::vector<FunctionIdentifier>>& threadStacks)
{
    prof->AllocateBuffer();
    prof->cur_cpu_writer_->StartBatch();

    for (const auto& [threadId, threadStack] : threadStacks)
    {
        if (threadStack.empty())
        {
            continue;
        }

        thread_span_context spanContext = GetContext(threadId);
        const auto          threadState = GetThreadState(prof->managed_tid_to_state_, threadId);

        prof->cur_cpu_writer_->StartSample(threadId, threadState, spanContext);

        if (prof->selectedThreadsSamplingInterval.has_value())
        {
            const auto threadSelectedForFrequentSampling =
                selective_sampling_trace_map.find(spanContext.trace_context_) != selective_sampling_trace_map.end();
            prof->cur_cpu_writer_->MarkSelectedForFrequentSampling(threadSelectedForFrequentSampling);
        }

        ResolveFrames(prof, threadStack, *prof->cur_cpu_writer_);
        prof->cur_cpu_writer_->EndSample();
    }
    prof->cur_cpu_writer_->EndBatch();

    prof->cur_cpu_writer_->WriteFinalStats(prof->stats_);

    prof->PublishBuffer();
}

static void ResolveSymbolsAndPublishBufferForSelectedThreads(
    ContinuousProfiler* prof, const std::unordered_map<ThreadID, std::vector<FunctionIdentifier>>& threadStacks)
{
    std::vector<unsigned char> localBytes;
    ThreadSamplesBuffer        localBuf(&localBytes);

    localBuf.StartSelectedThreadsBatch();
    for (const auto& [threadId, threadStack] : threadStacks)
    {
        if (threadStack.empty())
        {
            continue;
        }

        thread_span_context spanContext = GetContext(threadId);
        const auto          threadState = GetThreadState(prof->managed_tid_to_state_, threadId);

        localBuf.StartSampleForSelectedThread(threadState, spanContext);
        ResolveFrames(prof, threadStack, localBuf);
        localBuf.EndSample();
    }
    localBuf.EndSelectedThreadsBatch();

    // TODO: write out stats
    AppendToSelectedThreadsSampleBuffer(static_cast<int32_t>(localBytes.size()), localBytes.data());
}

static void RemoveOutdatedEntries(std::unordered_map<trace_context, long long>& selectiveSamplingTraceSet,
                                  ContinuousProfiler*                           prof)
{
    const auto now = std::chrono::steady_clock::now();

    if (prof->nextOutdatedEntriesScan > now)
    {
        return;
    }

    auto nextScan = now + std::chrono::minutes(kSelectiveSamplingMaxAgeMinutes);
    // Remove entries queued more than kSelectiveSamplingMaxAgeMinutes minutes ago.
    for (auto it = selectiveSamplingTraceSet.begin(); it != selectiveSamplingTraceSet.end();)
    {
        const auto deadline = std::chrono::time_point<std::chrono::steady_clock>(std::chrono::milliseconds(it->second));
        if (now >= deadline)
        {
            trace::Logger::Warn("SelectiveSampling: removing outdated entry for trace {",
                                "traceIdHigh: ", it->first.trace_id_high_, ", traceIdLow: ", it->first.trace_id_low_,
                                "} because it was enqueued more than ", kSelectiveSamplingMaxAgeMinutes,
                                " minutes ago");
            it = selectiveSamplingTraceSet.erase(it);
        }
        else
        {
            if (deadline < nextScan)
            {
                nextScan = deadline;
            }
            ++it;
        }
    }
    prof->nextOutdatedEntriesScan = nextScan;
}

static void PauseClrAndCaptureSamples(ContinuousProfiler*                                            prof,
                                      ICorProfilerInfo12*                                            info12,
                                      const SamplingType                                             samplingType,
                                      std::unordered_map<ThreadID, std::vector<FunctionIdentifier>>& threadStacksBuffer)
{
    // before trying to suspend the runtime, acquire exclusive lock
    // it's not safe to try to suspend the runtime after other locks are acquired
    // if there is application thread in the middle of AllocationTick
    std::unique_lock<std::shared_mutex> unique_lock(profiling_lock);

    // These locks are in use by managed threads; Acquire locks before suspending the runtime to prevent deadlock
    // Any of these can be in use by random app/clr threads, but this is the only
    // place that acquires more than one lock at a time.
    std::lock_guard<std::mutex> thread_state_guard(prof->thread_state_lock_);
    std::lock_guard<std::mutex> span_context_guard(thread_span_context_lock);
    std::lock_guard<std::mutex> name_cache_guard(name_cache_lock);

    // Selective sampling lock
    std::lock_guard<std::mutex> selective_sampling_guard(selective_sampling_lock);

    if (prof->IsShutdownRequested())
    {
        return;
    }
    // Checks to avoid unnecessary suspends.
    if (samplingType == SamplingType::SelectedThreads)
    {
        RemoveOutdatedEntries(selective_sampling_trace_map, prof);
        if (selective_sampling_trace_map.empty())
        {
            return;
        }

        selective_sampling_thread_buffer.clear();
        for (const auto& [traceContext, _] : selective_sampling_trace_map)
        {
            thread_span_context_map.GetAllThreads(traceContext, selective_sampling_thread_buffer);
        }
        if (selective_sampling_thread_buffer.empty())
        {
            return;
        }
    }
    if (samplingType == SamplingType::Continuous)
    {
        const bool shouldSample = ThreadSamplingShouldProduceThreadSample();
        if (!shouldSample)
        {
            trace::Logger::Warn(
                "Skipping a thread sample period, buffers are full. ** THIS WILL RESULT IN LOSS OF PROFILING DATA **");
            return;
        }
    }

    prof->stats_ = SamplingStatistics();

    const auto start = std::chrono::steady_clock::now();

    HRESULT hr = info12->SuspendRuntime();

    if (FAILED(hr))
    {
        trace::Logger::Warn("Could not suspend runtime to sample threads. HRESULT=0x", std::setfill('0'), std::setw(8),
                            std::hex, hr);
    }
    else
    {
        try
        {

            if (samplingType == SamplingType::Continuous)
            {
                auto allThreads = EnumerateThreads(info12);
                CaptureFunctionIdentifiersForThreads(prof, info12, allThreads, threadStacksBuffer);
            }
            else if (samplingType == SamplingType::SelectedThreads)
            {
                CaptureFunctionIdentifiersForThreads(prof, info12, selective_sampling_thread_buffer,
                                                     threadStacksBuffer);
            }
        }
        catch (const std::exception& e)
        {
            trace::Logger::Warn("Could not capture thread samples: ", e.what());
        }
        catch (...)
        {
            trace::Logger::Warn("Could not capture thread sample for unknown reasons");
        }
    }
    // I don't have any proof but I sure hope that if suspending fails then it's still ok to ask to resume, with no
    // ill effects
    hr = info12->ResumeRuntime();

    const auto end            = std::chrono::steady_clock::now();
    const auto elapsed_micros = std::chrono::duration_cast<std::chrono::microseconds>(end - start).count();

    prof->stats_.micros_suspended = static_cast<int>(elapsed_micros);

    if (FAILED(hr))
    {
        trace::Logger::Error("Could not resume runtime? HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
    }

    const size_t nonEmptyCount = std::count_if(threadStacksBuffer.begin(), threadStacksBuffer.end(),
                                               [](const std::pair<const ThreadID, std::vector<FunctionIdentifier>>& v)
                                               { return !v.second.empty(); });

    // Return early if all the buckets are empty
    if (nonEmptyCount == 0)
    {
        return;
    }

    prof->stats_.num_threads = static_cast<int>(nonEmptyCount);

    if (samplingType == SamplingType::Continuous)
    {
        ResolveSymbolsAndPublishBufferForAllThreads(prof, threadStacksBuffer);
    }
    else if (samplingType == SamplingType::SelectedThreads)
    {
        ResolveSymbolsAndPublishBufferForSelectedThreads(prof, threadStacksBuffer);
    }
}

static unsigned int GetSleepTime(const ContinuousProfiler* const prof)
{
    // Assumption is continuous profiling interval is bigger and multiple of selective sampling interval.
    // If both are enabled, we need to wake every smaller interval.
    if (prof->selectedThreadsSamplingInterval.has_value())
    {
        return prof->selectedThreadsSamplingInterval.value();
    }
    if (prof->threadSamplingInterval.has_value())
    {
        return prof->threadSamplingInterval.value();
    }
    // Shouldn't ever happen.
    return 0;
}

static void ReserveCapacity(ContinuousProfiler* const                                      prof,
                            std::unordered_map<ThreadID, std::vector<FunctionIdentifier>>& threadStacksBuffer)
{
    threadStacksBuffer.reserve(50);
    std::lock_guard<std::mutex> thread_state_guard(prof->thread_state_lock_);
    for (auto& [threadId, _] : prof->managed_tid_to_state_)
    {
        threadStacksBuffer[threadId].reserve(80);
    }
}

static std::chrono::steady_clock::time_point GetNextRefreshTime(
    const std::chrono::time_point<std::chrono::steady_clock> now)
{
    return now + std::chrono::minutes(5);
}

static bool ShouldTrackIterations(const ContinuousProfiler* const prof)
{
    return prof->selectedThreadsSamplingInterval.has_value() && prof->threadSamplingInterval.has_value();
}

static void SamplingThreadMain(ContinuousProfiler* prof)
{
    ICorProfilerInfo12* info12 = prof->info12;

    info12->InitializeCurrentThread();

    std::unordered_map<ThreadID, std::vector<FunctionIdentifier>> threadStacksBuffer;
    unsigned int                                                  iteration = 0;
    ReserveCapacity(prof, threadStacksBuffer);

    const auto startTime    = std::chrono::steady_clock::now();
    auto       next_refresh = GetNextRefreshTime(startTime);

    while (!prof->IsShutdownRequested())
    {
        if (ShouldTrackIterations(prof))
        {
            iteration++;
        }

        const unsigned int sleepTime = GetSleepTime(prof);
        if (sleepTime == 0)
        {
            trace::Logger::Warn("Unexpected sampling interval configured, exiting sampling thread.");
            return;
        }

        {
            std::unique_lock<std::mutex> lock(prof->shutdown_mutex_);
            if (prof->shutdown_cv_.wait_for(lock, std::chrono::milliseconds(sleepTime),
                                            [prof] { return prof->IsShutdownRequested(); }))
            {
                return; // Shutdown signaled
            }
        }

        const auto samplingType = GetNextSamplingType(prof, iteration);

        if (samplingType == SamplingType::Continuous && ShouldTrackIterations(prof))
        {
            iteration = 0;
        }

        PauseClrAndCaptureSamples(prof, info12, samplingType, threadStacksBuffer);

        if (prof->IsShutdownRequested())
        {
            return;
        }

        const auto now = std::chrono::steady_clock::now();
        // In order to avoid the need to recreate the vectors each iteration,
        // for the most of the iterations, instead of clearing the map, clear only the vectors.
        if (now > next_refresh)
        {
            threadStacksBuffer.clear();
            ReserveCapacity(prof, threadStacksBuffer);
            next_refresh = GetNextRefreshTime(now);
        }
        else
        {
            for (auto& [_, threadBuffer] : threadStacksBuffer)
            {
                threadBuffer.clear();
            }
        }
    }
}

void ContinuousProfiler::SetGlobalInfo12(ICorProfilerInfo12* cor_profiler_info12)
{
    profiler_info        = cor_profiler_info12;
    this->info12         = cor_profiler_info12;
    this->helper.info12_ = cor_profiler_info12;
}

void ContinuousProfiler::InitSelectiveSamplingBuffer()
{
    selective_sampling_buffer.reserve(kSamplesBufferDefaultSize);
    selective_sampling_thread_buffer.reserve(kSelectiveSamplingMaxTraces);
}

void ContinuousProfiler::StartThreadSampling()
{
    thread_sampling_thread_ = std::make_unique<std::thread>(SamplingThreadMain, this);
}

void ContinuousProfiler::Shutdown()
{
    shutdown_requested_.store(true, std::memory_order_release);
    shutdown_cv_.notify_all();
    {
        std::unique_lock<std::shared_mutex> unique_lock(profiling_lock);
        StopAllocationSampling();
    }

    if (thread_sampling_thread_ != nullptr && thread_sampling_thread_->joinable())
    {
        thread_sampling_thread_->join();
        thread_sampling_thread_.reset();
        trace::Logger::Info("ContinuousProfiler sampling thread stopped.");
    }
}

bool ContinuousProfiler::IsShutdownRequested() const
{
    return shutdown_requested_.load(std::memory_order_acquire);
}

static thread_span_context GetCurrentSpanContext(ThreadID tid)
{
    std::lock_guard<std::mutex> guard(thread_span_context_lock);
    return GetContext(tid);
}

ThreadState* ContinuousProfiler::GetCurrentThreadState(ThreadID tid)
{
    std::lock_guard<std::mutex> guard(thread_state_lock_);
    return managed_tid_to_state_[tid];
}

// You can read about the ETW event format for AllocationTick at
// https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events#gcallocationtick_v3-event
// or, if that is not working, a search for "GCAllocationTick ETW" will get you there.
// As of this comment, the above link only documents v3 of the event, with v4 undocumented but
// by source traversal differs only by the addition of the actual size of the just-allocated object
// Do not be fooled by "AllocationAmount" which is set to the 100kb sampling limit.

// https://github.com/dotnet/runtime/blob/cdb6e1d5f9075214c8a58ca75d5314b5dc64daed/src/coreclr/vm/ClrEtwAll.man#L1157

// AllocationAmount     int32
// AllocationKind       int32
// InstanceId           int16
// AllocationAmount64   int64
// TypeId               pointer
// TypeName             ucs2 string, null terminated, variable length
// HeapIndex            int32
// Address              pointer
// AllocatedSize        int64

constexpr auto EtwPointerSize                         = sizeof(void*);
constexpr auto AllocationTickV4TypeNameStartByteIndex = 4 + 4 + 2 + 8 + EtwPointerSize;
constexpr auto AllocationTickV4SizeWithoutTypeName    = 4 + 4 + 2 + 8 + EtwPointerSize + 4 + EtwPointerSize + 8;

static void CaptureAllocationStack(ContinuousProfiler* prof, std::vector<FunctionIdentifier>& threadStack)
{
    DoStackSnapshotParams doStackSnapshotParams(prof, &threadStack);
    HRESULT               hr = prof->info12->DoStackSnapshot((ThreadID)NULL, &FrameCallback, COR_PRF_SNAPSHOT_DEFAULT,
                                                             &doStackSnapshotParams, nullptr, 0);
    if (FAILED(hr))
    {
        trace::Logger::Debug("DoStackSnapshot failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
    }
}

AllocationSubSampler::AllocationSubSampler(uint32_t targetPerCycle_, uint32_t secondsPerCycle_)
    : targetPerCycle(targetPerCycle_)
    , secondsPerCycle(secondsPerCycle_)
    , seenThisCycle(0)
    , sampledThisCycle(0)
    , seenLastCycle(0)
    , nextCycleStartMillis(
          std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()))
    , sampleLock()
    , rand(std::default_random_engine((unsigned int)(nextCycleStartMillis.count())))
{
}
void AllocationSubSampler::AdvanceCycle(std::chrono::milliseconds now)
{
    nextCycleStartMillis = now + std::chrono::seconds(secondsPerCycle);
    seenLastCycle        = seenThisCycle;
    seenThisCycle        = 0;
    sampledThisCycle     = 0;
}

// We want to sample T items out of N per unit time, where N is unknown and may be < T or may be orders
// of magnitude bigger than T.  One excellent approach for this is reservoir sampling, where new items
// displace existing samples such that the end result is a uniform sample of N.  However, our overhead is not
// just limited to the subscription to the AllocationTick events, but also the additional
// captured data (e.g., the stack trace, locking and copying the span context).  Therefore, reservoir "replacements"
// where an already-captured item gets displaced by a new one add additional undesired overhead.  How much?
// Well, some monte carlo experiments with (e.g.) T=100 and N=1000 suggest that the wasted overhead on unsent data
// would be Waste~=230, a tremendous waste of CPU cycles to collect and then discard 230 stack traces, etc.
// Instead, let's treat the current cycle as statistically very similar to the last one, and sample 1/X events
// where X is based on what N was last time.  Not the most elegant approach, but simple to code and errs on the
// side of reduced/capped overhead.
bool AllocationSubSampler::ShouldSample()
{
    std::lock_guard<std::mutex> guard(sampleLock);

    auto now =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    if (now > nextCycleStartMillis)
    {
        AdvanceCycle(now);
    }
    seenThisCycle++;
    if (sampledThisCycle >= targetPerCycle)
    {
        return false;
    }
    // roll a [1,lastCycle] die, and if it comes up <= targetPerCycle, it wins
    // But lastCycle could be 0, so normalize that to 1.
    std::uniform_int_distribution<uint32_t> rando(1, std::max(seenLastCycle, (uint32_t)1));
    bool                                    sample = rando(rand) <= targetPerCycle;
    if (sample)
    {
        sampledThisCycle++;
    }
    return sample;
}

void ContinuousProfiler::AllocationTick(ULONG dataLen, LPCBYTE data)
{
    // try to acquire shared lock without blocking
    // and return early if attempt was unsuccessful -
    // PauseClrAndCaptureSamples acquired exclusive lock
    // and it's not safe to proceed
    std::shared_lock<std::shared_mutex> shared_lock(profiling_lock, std::try_to_lock);
    if (!shared_lock.owns_lock())
    {
        // can't continue if suspension already started
        trace::Logger::Debug("Possible runtime suspension in progress, can't safely process allocation tick.");
        return;
    }

    if (IsShutdownRequested())
    {
        return;
    }

    if (this->allocationSubSampler == nullptr || !this->allocationSubSampler->ShouldSample())
    {
        return;
    }

    // In v4 it's the last field, so use a relative offset from the end
    uint64_t allocatedSize = *((uint64_t*)&(data[dataLen - 8]));
    // Here's the first byte of the typeName
    WCHAR* typeName = (WCHAR*)&data[AllocationTickV4TypeNameStartByteIndex];

    // and its length can be derived without iterating it since there is only the one variable-length field
    // account for the null char
    size_t typeNameCharLen = (dataLen - AllocationTickV4SizeWithoutTypeName) / 2 - 1;

    ThreadID      threadId;
    const HRESULT hr = info12->GetCurrentThreadID(&threadId);
    if (FAILED(hr))
    {
        trace::Logger::Debug("GetCurrentThreadId failed, ", hr);
        return;
    }

    // TODO: not used at the moment, setting for consistency
    this->stats_ = SamplingStatistics();

    auto unknownThreadState = ThreadState();
    auto spanCtx            = GetCurrentSpanContext(threadId);
    auto threadState        = GetCurrentThreadState(threadId);
    if (threadState == nullptr)
    {
        threadState = &unknownThreadState;
    }
    // Note that by using a local buffer that we will copy as a whole into the
    // "main" one later, we gain atomicity and improved concurrency, but lose out on a shared
    // string-coding dictionary for all the allocation samples in a cycle.  The tradeoffs here
    // are non-obvious and the code+locking complexity to share codes would be high, so this will do
    // until proven otherwise.  The managed code specifically understands that the strings in each
    // allocation sample are coded separately so if this changes, that code will need to change too.
    std::vector<unsigned char> localBytes;
    ThreadSamplesBuffer        localBuf = ThreadSamplesBuffer(&localBytes);
    localBuf.AllocationSample(allocatedSize, typeName, typeNameCharLen, threadId, threadState, spanCtx);

    std::vector<FunctionIdentifier> threadStack;

    {
        std::lock_guard<std::mutex> guard(name_cache_lock);
        this->helper.ClearFunctionIdentifierCache();

        CaptureAllocationStack(this, threadStack);
        ResolveFrames(this, threadStack, localBuf);
    }

    localBuf.EndSample();
    AllocationSamplingAppendToBuffer(static_cast<int32_t>(localBytes.size()), localBytes.data());
}

void ContinuousProfiler::StartAllocationSampling(const unsigned int maxMemorySamplesPerMinute)
{
    this->allocationSubSampler = std::make_unique<AllocationSubSampler>(maxMemorySamplesPerMinute, 60);

    COR_PRF_EVENTPIPE_PROVIDER_CONFIG sessionConfig[] = {{WStr("Microsoft-Windows-DotNETRuntime"),
                                                          0x1, // CLR_GC_KEYWORD
                                                          // documentation says AllocationTick is at info but it lies
                                                          COR_PRF_EVENTPIPE_VERBOSE, nullptr}};
    HRESULT                           hr = this->info12->EventPipeStartSession(1, sessionConfig, false, &session_);
    if (FAILED(hr))
    {
        trace::Logger::Error("Could not enable allocation sampling: session pipe error", hr);
    }

    trace::Logger::Info("ContinuousProfiler::MemoryProfiling started.");
}

void ContinuousProfiler::StopAllocationSampling()
{
    if (session_ == 0)
    {
        return;
    }

    HRESULT hr = this->info12->EventPipeStopSession(session_);
    if (FAILED(hr))
    {
        trace::Logger::Error("Could not disable allocation sampling: session pipe error. HRESULT=0x", std::setfill('0'),
                             std::setw(8), std::hex, hr);
    }
    session_ = 0;

    trace::Logger::Info("ContinuousProfiler allocation sampling session stopped.");
}

void ContinuousProfiler::ThreadCreated(ThreadID thread_id)
{
    // So it seems the Thread* items can be/are called out of order.  ThreadCreated doesn't carry any valuable
    // ThreadState information so this is a deliberate nop.  The other methods will fault in ThreadStates
    // as needed.
    // Hopefully the destroyed event is not called out of order with the others... if so, the worst that happens
    // is we get an empty name string and a 0 in the native ID column
}
void ContinuousProfiler::ThreadDestroyed(ThreadID thread_id)
{
    {
        std::lock_guard<std::mutex> guard(thread_state_lock_);

        const ThreadState* state = managed_tid_to_state_[thread_id];

        delete state;

        managed_tid_to_state_.erase(thread_id);
    }
    {
        std::lock_guard<std::mutex> guard(thread_span_context_lock);

        thread_span_context_map.Remove(thread_id);
    }
}
void ContinuousProfiler::ThreadNameChanged(ThreadID thread_id, ULONG cch_name, WCHAR name[])
{
    std::lock_guard<std::mutex> guard(thread_state_lock_);

    ThreadState* state = managed_tid_to_state_[thread_id];
    if (state == nullptr)
    {
        state                            = new ThreadState();
        managed_tid_to_state_[thread_id] = state;
    }
    state->thread_name_.clear();
    state->thread_name_.append(name, cch_name);
}

template <typename TKey, typename TValue>
NameCache<TKey, TValue>::NameCache(const size_t maximum_size, const TValue default_value)
    : max_size_(maximum_size), default_value_(default_value)
{
}

template <typename TKey, typename TValue>
TValue NameCache<TKey, TValue>::Get(TKey key)
{
    const auto found = map_.find(key);
    if (found == map_.end())
    {
        return default_value_;
    }
    // This voodoo moves the single item in the iterator to the front of the list
    // (as it is now the most-recently-used)
    list_.splice(list_.begin(), list_, found->second);
    return found->second->second;
}

template <typename TKey, typename TValue>
TValue NameCache<TKey, TValue>::Put(TKey key, TValue val)
{
    const auto pair = std::pair(key, val);
    list_.push_front(pair);
    map_[key] = list_.begin();

    if (map_.size() > max_size_)
    {
        const auto& lru       = list_.back();
        const auto  old_value = lru.second;
        map_.erase(lru.first);
        list_.pop_back();
        return old_value;
    }
    return default_value_;
}

template <typename TKey, typename TValue>
void NameCache<TKey, TValue>::Clear()
{
    map_.clear();
    list_.clear();
}

} // namespace continuous_profiler

extern "C"
{
    EXPORTTHIS int32_t ContinuousProfilerReadThreadSamples(int32_t len, unsigned char* buf)
    {
        return ThreadSamplingConsumeOneThreadSample(len, buf);
    }
    EXPORTTHIS int32_t ContinuousProfilerReadAllocationSamples(int32_t len, unsigned char* buf)
    {
        return AllocationSamplingConsumeAndReplaceBuffer(len, buf);
    }
    EXPORTTHIS int32_t SelectiveSamplerReadThreadSamples(int32_t len, unsigned char* buf)
    {
        return SelectiveSamplingConsumeAndClearBuffer(len, buf);
    }
    EXPORTTHIS void ContinuousProfilerNotifySpanStopped(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId)
    {
        std::lock_guard<std::mutex>                    guard(thread_span_context_lock);
        const continuous_profiler::thread_span_context spanContext = {traceIdHigh, traceIdLow, spanId};
        thread_span_context_map.Remove(spanContext);
    }
    EXPORTTHIS void ContinuousProfilerSetNativeContext(uint64_t traceIdHigh, uint64_t traceIdLow, uint64_t spanId)
    {
        // This method is called anytime thread-span association changes, e.g. when activity is started/stopped or
        // when suspension/resumption occurs.

        ThreadID      threadId;
        const HRESULT hr = profiler_info->GetCurrentThreadID(&threadId);
        if (FAILED(hr))
        {
            trace::Logger::Debug("GetCurrentThreadID failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex,
                                 hr);
            return;
        }
        std::lock_guard<std::mutex> guard(thread_span_context_lock);

        const continuous_profiler::thread_span_context newSpanContext = {traceIdHigh, traceIdLow, spanId};
        thread_span_context_map.Put(threadId, newSpanContext);
    }
    EXPORTTHIS void SelectiveSamplingStart(uint64_t traceIdHigh, uint64_t traceIdLow)
    {
        {
            std::lock_guard<std::mutex> guard(selective_sampling_lock);

            if (profiler_info == nullptr)
            {
                return;
            }

            const continuous_profiler::trace_context context = {traceIdHigh, traceIdLow};
            if (context.IsDefault())
            {
                return;
            }

            // Don't allow for too many samples at once
            if (selective_sampling_trace_map.size() < kSelectiveSamplingMaxTraces)
            {
                const auto deadline =
                    std::chrono::steady_clock::now() + std::chrono::minutes(kSelectiveSamplingMaxAgeMinutes);
                selective_sampling_trace_map[context] =
                    std::chrono::duration_cast<std::chrono::milliseconds>(deadline.time_since_epoch()).count();
                return;
            }
        }
        trace::Logger::Warn("SelectiveSamplingStart: ignoring request to start sampling for trace {",
                            "traceIdHigh: ", traceIdHigh, ", traceIdLow: ", traceIdLow,
                            "} because maximum number of traces is already being sampled.");
    }

    EXPORTTHIS void SelectiveSamplingStop(uint64_t traceIdHigh, uint64_t traceIdLow)
    {
        if (profiler_info == nullptr)
        {
            return;
        }

        const continuous_profiler::trace_context context = {traceIdHigh, traceIdLow};
        if (context.IsDefault())
        {
            return;
        }
        std::lock_guard<std::mutex> guard(selective_sampling_lock);
        selective_sampling_trace_map.erase(context);
    }
}
