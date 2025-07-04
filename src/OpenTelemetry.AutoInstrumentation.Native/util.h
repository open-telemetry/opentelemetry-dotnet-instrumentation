/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_UTIL_H_
#define OTEL_CLR_PROFILER_UTIL_H_

#include <algorithm>
#include <condition_variable>
#include <mutex>
#include <queue>
#include <sstream>
#include <string>
#include <thread>
#include <unordered_map>
#include <vector>

#include "string_utils.h"

namespace trace
{

// Split splits a string by the given delimiter.
std::vector<WSTRING> Split(const WSTRING& s, wchar_t delim);

// Trim removes space from the beginning and end of a string.
WSTRING Trim(const WSTRING& str);

// GetEnvironmentValue returns the environment variable value for the given
// name. Space is trimmed.
WSTRING GetEnvironmentValue(const WSTRING& name);

// GetConfiguredSize returns the environment variable value for the given name, or default value
// if not configured, or misconfigured
size_t GetConfiguredSize(const WSTRING& name, size_t default_value);

// GetEnvironmentValues returns environment variable values for the given name
// split by the delimiter. Space is trimmed and empty values are ignored.
std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name, const wchar_t delim);

// GetEnvironmentValues calls GetEnvironmentValues with a semicolon delimiter.
std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name);

// GetEnvironmentVariables returns list of all environment variable
std::vector<WSTRING> GetEnvironmentVariables(const std::vector<WSTRING> &prefixes);

// Convert Hex to string
WSTRING HexStr(const void* data, int len);

// Convert Token to string
WSTRING TokenStr(const mdToken* token);

// Convert HRESULT to a friendly string, e.g.: "0x80000002"
WSTRING HResultStr(const HRESULT hr);

WSTRING VersionStr(const USHORT major, const USHORT minor, const USHORT build, const USHORT revision);

WSTRING AssemblyVersionStr(const ASSEMBLYMETADATA& assembly_metadata);

template <class Container>
bool Contains(const Container& items, const typename Container::value_type& value)
{
    return std::find(items.begin(), items.end(), value) != items.end();
}

// Singleton definition
class UnCopyable
{
protected:
    UnCopyable(){};
    ~UnCopyable(){};

private:
    UnCopyable(const UnCopyable&) = delete;
    UnCopyable(const UnCopyable&&) = delete;
    UnCopyable& operator=(const UnCopyable&) = delete;
    UnCopyable& operator=(const UnCopyable&&) = delete;
};

template <typename T>
class Singleton : public UnCopyable
{
public:
    static T* Instance()
    {
        static T instance_obj;
        return &instance_obj;
    }
};

template <typename T>
class BlockingQueue : public UnCopyable
{
private:
    std::queue<T> queue_;
    mutable std::mutex mutex_;
    std::condition_variable condition_;

public:
    T pop()
    {
        std::unique_lock<std::mutex> mlock(mutex_);
        while (queue_.empty())
        {
            condition_.wait(mlock);
        }
        T value = queue_.front();
        queue_.pop();
        return value;
    }
    void push(const T& item)
    {
        {
            std::lock_guard<std::mutex> guard(mutex_);
            queue_.push(item);
        }
        condition_.notify_one();
    }
};

template <typename T>
class UniqueBlockingQueue : public UnCopyable
{
private:
    std::queue<std::unique_ptr<T>> queue_;
    mutable std::mutex mutex_;
    std::condition_variable condition_;

public:
    std::unique_ptr<T> pop()
    {
        std::unique_lock<std::mutex> mlock(mutex_);
        while (queue_.empty())
        {
            condition_.wait(mlock);
        }
        std::unique_ptr<T> value = std::move(queue_.front());
        queue_.pop();
        return value;
    }
    void push(std::unique_ptr<T>&& item)
    {
        {
            std::lock_guard<std::mutex> guard(mutex_);
            queue_.push(std::move(item));
        }
        condition_.notify_one();
    }
};

} // namespace trace

#endif // OTEL_CLR_PROFILER_UTIL_H_
