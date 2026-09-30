/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_STRING_H_
#define OTEL_CLR_PROFILER_STRING_H_

#include <corhlpr.h>
#include <iomanip>
#include <sstream>
#include <string>

#ifdef _WIN32
#define WStr(value) L##value
#define WStrLen(value) (size_t) wcslen(value)
#else
#define WStr(value) u##value
#define WStrLen(value) (size_t) std::char_traits<char16_t>::length(value)
#endif

namespace trace
{

std::string PadLeft(const std::string& txt, std::size_t len, char c = ' ');

inline std::string Hex(ULONG value, int padding = 8, std::string prefix = "0x")
{
    std::stringstream str;
    str << prefix << std::hex << std::uppercase << std::right << std::setfill('0') << std::setw(padding)
        << (ULONG)value;
    return str.str();
}

typedef std::basic_string<WCHAR> WSTRING;

#ifndef MACOS
typedef std::basic_stringstream<WCHAR> WSTRINGSTREAM;
#endif

std::string ToString(const std::string& str);
std::string ToString(const char* str);
std::string ToString(uint64_t i);
std::string ToString(const WSTRING& wstr);
std::string ToString(const WCHAR* wstr);
std::string ToString(const WCHAR* wstr, std::size_t nbChars);
std::string ToString(const GUID& uid);

WSTRING ToWSTRING(const std::string& str);
WSTRING ToWSTRING(uint64_t i);

static const WSTRING EmptyWStr = WStr("");

} // namespace trace

#endif // OTEL_CLR_PROFILER_STRING_H_
