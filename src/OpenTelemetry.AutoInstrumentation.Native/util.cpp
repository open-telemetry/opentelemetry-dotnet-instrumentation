// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "util.h" // Keep first to avoid PCH warning.

#include <cwctype>
#include <iomanip>
#include <iterator>
#include <unordered_map>
#include <string>
#include <vector>

#include "environment_variables_parser.h"
#include "pal.h"
#include "string.h"

#ifdef MACOS
extern char** environ;
#endif

namespace trace
{

template <typename Out>
void Split(const WSTRING& s, wchar_t delim, Out result)
{
    size_t lpos = 0;
    for (size_t i = 0; i < s.length(); i++)
    {
        if (s[i] == delim)
        {
            *(result++) = s.substr(lpos, (i - lpos));
            lpos        = i + 1;
        }
    }
    *(result++) = s.substr(lpos);
}

std::vector<WSTRING> Split(const WSTRING& s, wchar_t delim)
{
    std::vector<WSTRING> elems;
    Split(s, delim, std::back_inserter(elems));
    return elems;
}

WSTRING Trim(const WSTRING& str)
{
    if (str.length() == 0)
    {
        return EmptyWStr;
    }

    WSTRING trimmed = str;

    auto lpos = trimmed.find_first_not_of(WStr(" \t"));
    if (lpos != WSTRING::npos && lpos > 0)
    {
        trimmed = trimmed.substr(lpos);
    }

    auto rpos = trimmed.find_last_not_of(WStr(" \t"));
    if (rpos != WSTRING::npos)
    {
        trimmed = trimmed.substr(0, rpos + 1);
    }

    return trimmed;
}

WSTRING GetEnvironmentValue(const WSTRING& name)
{
#ifdef _WIN32
    const size_t max_buf_size = 4096;
    WSTRING      buf(max_buf_size, 0);
    auto         len = GetEnvironmentVariable(name.data(), buf.data(), (DWORD)(buf.size()));
    return Trim(buf.substr(0, len));
#else
    auto cstr = std::getenv(ToString(name).c_str());
    if (cstr == nullptr)
    {
        return EmptyWStr;
    }
    std::string str(cstr);
    auto        wstr = ToWSTRING(str);
    return Trim(wstr);
#endif
}

std::string GetEnvironmentValueString(const WSTRING& name)
{
#ifdef _WIN32
    const size_t max_buf_size = 4096;
    WSTRING      buf(max_buf_size, 0);
    auto         len    = GetEnvironmentVariable(name.data(), buf.data(), static_cast<DWORD>(buf.size()));
    auto         string = ToString(buf.substr(0, len));
    return string;
#else
    auto cstr = std::getenv(ToString(name).c_str());
    if (cstr == nullptr)
    {
        return {};
    }
    auto string = std::string(cstr);
    return string;
#endif
}

size_t GetConfiguredSize(const WSTRING& name, const size_t default_value)
{
    try
    {
        const auto configured_value = GetEnvironmentValueString(name);
        if (configured_value.empty())
        {
            return default_value;
        }
        const auto   converted  = std::stoll(configured_value);
        const size_t max_size_t = (size_t)-1;
        if (converted < 0 || converted > max_size_t)
        {
            return default_value;
        }
        return static_cast<size_t>(converted);
    }
    catch (...)
    {
        return default_value;
    }
}

std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name, const wchar_t delim)
{
    std::vector<WSTRING> values;
    for (auto s : Split(GetEnvironmentValue(name), delim))
    {
        s = Trim(s);
        if (!s.empty())
        {
            values.push_back(s);
        }
    }
    return values;
}

std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name)
{
    return GetEnvironmentValues(name, L',');
}

std::vector<WSTRING> GetEnvironmentVariables(const std::vector<WSTRING>& prefixes)
{
    std::vector<WSTRING> env_strings;
#ifdef _WIN32
    // Documentation for GetEnvironmentStrings:
    // https://learn.microsoft.com/en-us/windows/win32/api/processenv/nf-processenv-getenvironmentstrings#remarks
    const auto env_variables = GetEnvironmentStrings();
    int        prev          = 0;
    for (int i = 0;; i++)
    {
        if (env_variables[i] != '\0')
        {
            continue;
        }

        auto env_variable = WSTRING(env_variables + prev, env_variables + i);
        for (const auto& prefix : prefixes)
        {
            if (env_variable.find(prefix) == 0)
            {
                env_strings.push_back(env_variable);
                break;
            }
        }

        prev = i + 1;
        if (env_variables[i + 1] == '\0')
        {
            break;
        }
    }

    FreeEnvironmentStrings(env_variables);
#else
    for (char** current = environ; *current; current++)
    {
        auto env_variable = ToWSTRING(ToString(*current));
        for (const auto& prefix : prefixes)
        {
            if (env_variable.find(prefix) == 0)
            {
                env_strings.push_back(env_variable);
                break;
            }
        }
    }
#endif
    return env_strings;
}

constexpr char HexMap[] = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};

WSTRING HexStr(const void* dataPtr, int len)
{
    const unsigned char* data = (unsigned char*)dataPtr;
    WSTRING              s(len * 2, ' ');
    for (int i = 0; i < len; ++i)
    {
        s[2 * i]     = HexMap[(data[i] & 0xF0) >> 4];
        s[2 * i + 1] = HexMap[data[i] & 0x0F];
    }
    return s;
}

WSTRING TokenStr(const mdToken* token)
{
    const unsigned char* data = (unsigned char*)token;
    int                  len  = sizeof(mdToken);
    WSTRING              s(len * 2, ' ');
    for (int i = 0; i < len; i++)
    {
        s[(2 * (len - i)) - 2] = HexMap[(data[i] & 0xF0) >> 4];
        s[(2 * (len - i)) - 1] = HexMap[data[i] & 0x0F];
    }
    return s;
}

WSTRING HResultStr(const HRESULT hr)
{
    std::stringstream ss;
    ss << "0x" << std::setfill('0') << std::setw(2 * sizeof(HRESULT)) << std::hex << hr;

    return ToWSTRING(ss.str());
}

WSTRING VersionStr(const USHORT major, const USHORT minor, const USHORT build, const USHORT revision)
{
    std::stringstream ss;
    ss << major << "." << minor << "." << build << "." << revision;
    return ToWSTRING(ss.str());
}

WSTRING AssemblyVersionStr(const ASSEMBLYMETADATA& assembly_metadata)
{
    return VersionStr(assembly_metadata.usMajorVersion, assembly_metadata.usMinorVersion,
                      assembly_metadata.usBuildNumber, assembly_metadata.usRevisionNumber);
}

} // namespace trace