// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "regex_utils.h"

#include <limits>
#include <regex>
#include <sstream>

// Include string utilities for conversion functions - after regex to avoid conflicts
#include "string_utils.h"

namespace trace
{

namespace
{

template <typename TString>
bool TryParseVersionComponent(const TString& component, unsigned short& value)
{
    unsigned int   parsed_value = 0;
    constexpr auto max_value    = (std::numeric_limits<unsigned short>::max)();

    if (component.empty())
    {
        return false;
    }

    for (const auto ch : component)
    {
        const auto digit_start = static_cast<typename TString::value_type>('0');
        const auto digit_end   = static_cast<typename TString::value_type>('9');

        if (ch < digit_start || ch > digit_end)
        {
            return false;
        }

        parsed_value = (parsed_value * 10) + static_cast<unsigned int>(ch - digit_start);
        if (parsed_value > max_value)
        {
            return false;
        }
    }

    value = static_cast<unsigned short>(parsed_value);
    return true;
}

template <typename TMatch>
bool TryParseVersionMatch(
    const TMatch& match, unsigned short& major, unsigned short& minor, unsigned short& build, unsigned short& revision)
{
    unsigned short parsed_major    = 0;
    unsigned short parsed_minor    = 0;
    unsigned short parsed_build    = 0;
    unsigned short parsed_revision = 0;

    if (!TryParseVersionComponent(match.str(1), parsed_major) ||
        !TryParseVersionComponent(match.str(2), parsed_minor) ||
        !TryParseVersionComponent(match.str(3), parsed_build) ||
        !TryParseVersionComponent(match.str(4), parsed_revision))
    {
        return false;
    }

    major    = parsed_major;
    minor    = parsed_minor;
    build    = parsed_build;
    revision = parsed_revision;
    return true;
}

// A public key token is 8 bytes, encoded in an assembly reference string as 16 hexadecimal
// characters (two hex characters per byte).
constexpr int kPublicKeyTokenBytes = 8;

// Converts a single hexadecimal character to its 0-15 value, returning false if it is not a hex
// digit. Templated on the character type so the same helper serves the wide (wchar_t) matches used
// on Windows and the narrow (char) matches used on Linux/macOS; the captured characters are ASCII
// in both cases.
template <typename TChar>
bool TryParseHexDigit(TChar ch, unsigned char& value)
{
    if (ch >= '0' && ch <= '9')
    {
        value = static_cast<unsigned char>(ch - '0');
        return true;
    }
    if (ch >= 'a' && ch <= 'f')
    {
        value = static_cast<unsigned char>(ch - 'a' + 10);
        return true;
    }
    if (ch >= 'A' && ch <= 'F')
    {
        value = static_cast<unsigned char>(ch - 'A' + 10);
        return true;
    }
    return false;
}

// Parses the capture group holding the public key token (16 hex characters) into
// kPublicKeyTokenBytes bytes. Templated on the regex sub-match type (std::wssub_match on Windows,
// std::ssub_match on Linux/macOS) so both platforms share the same logic. Writes nothing and
// returns false if the destination buffer is too small or the capture is not exactly 16 hex
// digits, so it never over-reads the match nor over-writes the buffer.
template <typename TSubMatch>
bool TryParsePublicKeyToken(const TSubMatch& sub_match, unsigned char* data, const int length)
{
    if (length < kPublicKeyTokenBytes)
    {
        return false;
    }

    if (!sub_match.matched || sub_match.length() != kPublicKeyTokenBytes * 2)
    {
        return false;
    }

    // Each byte is two hex characters: a high nibble followed by a low nibble.
    auto current_char = sub_match.first;
    for (int i = 0; i < kPublicKeyTokenBytes; i++)
    {
        unsigned char high;
        unsigned char low;
        if (!TryParseHexDigit(*current_char++, high) || !TryParseHexDigit(*current_char++, low))
        {
            return false;
        }

        data[i] = static_cast<unsigned char>((high << 4) | low);
    }

    return true;
}

} // namespace

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
        return TryParseVersionMatch(match, major, minor, build, revision);
    }
#else
    // On Linux/MacOS, convert to narrow string for regex
    std::string       narrow_str = ToString(str);
    static std::regex re("Version=([0-9]+)\\.([0-9]+)\\.([0-9]+)\\.([0-9]+)");
    std::smatch       match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 5)
    {
        return TryParseVersionMatch(match, major, minor, build, revision);
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

    // The regex captures the token as exactly 16 hexadecimal characters (capture group 1);
    // TryParsePublicKeyToken decodes those into kPublicKeyTokenBytes bytes. The extraction is
    // duplicated per platform only because the regex/match operate on the platform's native
    // character width - wide (std::wsmatch) on Windows and narrow (std::smatch, after converting to
    // UTF-8) on Linux/macOS - but both feed the same shared parsing helper.
#ifdef _WIN32
    static std::wregex re(WStr("PublicKeyToken=([a-fA-F0-9]{16})"));
    std::wsmatch       match;

    if (std::regex_search(str, match, re) && match.size() == 2)
    {
        return TryParsePublicKeyToken(match[1], data, length);
    }
#else
    // On Linux/MacOS, convert to narrow string for regex
    std::string       narrow_str = ToString(str);
    static std::regex re("PublicKeyToken=([a-fA-F0-9]{16})");
    std::smatch       match;

    if (std::regex_search(narrow_str, match, re) && match.size() == 2)
    {
        return TryParsePublicKeyToken(match[1], data, length);
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
