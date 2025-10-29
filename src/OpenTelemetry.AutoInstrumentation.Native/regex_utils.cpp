// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "regex_utils.h"

#include <regex>
#include <sstream>

// Include string utilities for conversion functions - after regex to avoid conflicts
#include "string_utils.h"

namespace trace
{

bool ExtractVersion(
    const WSTRING& str, unsigned short& major, unsigned short& minor, unsigned short& build, unsigned short& revision)
{
    if (str.empty())
    {
        return false;
    }

#ifdef _WIN32
    static std::wregex re(WStr("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)"));
    std::wsmatch       match;

    if (std::regex_search(str, match, re) && match.size() == 5)
    {
        WSTRINGSTREAM ss1(match.str(1));
        ss1 >> major;
        WSTRINGSTREAM ss2(match.str(2));
        ss2 >> minor;
        WSTRINGSTREAM ss3(match.str(3));
        ss3 >> build;
        WSTRINGSTREAM ss4(match.str(4));
        ss4 >> revision;
        return true;
    }
#else
    // On Linux/MacOS, convert to narrow string for regex
    std::string       narrow_str = ToString(str);
    static std::regex re("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)");
    std::smatch       match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 5)
    {
        major    = (unsigned short)std::stoi(match.str(1));
        minor    = (unsigned short)std::stoi(match.str(2));
        build    = (unsigned short)std::stoi(match.str(3));
        revision = (unsigned short)std::stoi(match.str(4));
        return true;
    }
#endif

    return false;
}

WSTRING ExtractCulture(const WSTRING& str)
{
    if (str.empty())
    {
        return WSTRING();
    }

#ifdef _WIN32
    static std::wregex re(WStr("Culture=([a-zA-Z0-9]+)"));
    std::wsmatch       match;

    if (std::regex_search(str, match, re) && match.size() == 2)
    {
        return match.str(1);
    }
#else
    // On Linux/MacOS, convert to narrow string for regex
    std::string       narrow_str = ToString(str);
    static std::regex re("Culture=([a-zA-Z0-9]+)");
    std::smatch       match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 2)
    {
        return ToWSTRING(match.str(1));
    }
#endif

    return WSTRING();
}

bool ExtractPublicKeyToken(const WSTRING& str, unsigned char* data, const int length)
{
    if (str.empty() || data == nullptr)
    {
        return false;
    }

#ifdef _WIN32
    static std::wregex re(WStr("PublicKeyToken=([a-fA-F0-9]{16})"));
    std::wsmatch       match;

    if (std::regex_search(str, match, re) && match.size() == 2)
    {
        for (int i = 0; i < length; i++)
        {
            auto          s = match.str(1).substr(i * 2, 2);
            unsigned long x;
            WSTRINGSTREAM ss(s);
            ss >> std::hex >> x;
            data[i] = (unsigned char)x;
        }
        return true;
    }
#else
    // On Linux/MacOS, convert to narrow string for regex
    std::string       narrow_str = ToString(str);
    static std::regex re("PublicKeyToken=([a-fA-F0-9]{16})");
    std::smatch       match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 2)
    {
        for (int i = 0; i < length; i++)
        {
            auto              s = match.str(1).substr(i * 2, 2);
            unsigned long     x;
            std::stringstream ss(s);
            ss >> std::hex >> x;
            data[i] = (unsigned char)x;
        }
        return true;
    }
#endif

    return false;
}

bool MatchesSecretsPattern(const std::string& str)
{
    if (str.empty())
    {
        return false;
    }

    // Pattern to match environment variables containing sensitive data
    // Matches: API, TOKEN, SECRET, KEY, PASSWORD, PASS, PWD, HEADER, CREDENTIALS
    // at word boundaries (preceded/followed by _ or start/end of string)
    static std::regex re("(?:^|_)(API|TOKEN|SECRET|KEY|PASSWORD|PASS|PWD|HEADER|CREDENTIALS)(?:_|$)",
                         std::regex_constants::ECMAScript | std::regex_constants::icase);

    return std::regex_search(str, re);
}

} // namespace trace
