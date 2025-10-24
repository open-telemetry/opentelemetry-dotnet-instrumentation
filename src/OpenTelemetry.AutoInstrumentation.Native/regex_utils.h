/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_REGEX_UTILS_H_
#define OTEL_CLR_PROFILER_REGEX_UTILS_H_

#include <string>

// Forward declare WSTRING to avoid including PAL headers
#ifdef _WIN32
typedef wchar_t WCHAR;
#else
typedef char16_t WCHAR;
#endif

namespace trace
{

typedef std::basic_string<WCHAR> WSTRING;

// Forward declarations for string conversion functions
std::string ToString(const WSTRING& wstr);
WSTRING ToWSTRING(const std::string& str);

// Regex utilities for parsing assembly references and matching patterns
// These functions work with WSTRING on all platforms

// Extracts version from assembly reference string (e.g., "Version=1.2.3.4")
// Returns true if version was found and parsed successfully
bool ExtractVersion(const WSTRING& str, unsigned short& major, unsigned short& minor, 
                    unsigned short& build, unsigned short& revision);

// Extracts culture/locale from assembly reference string (e.g., "Culture=en-US")
// Returns the culture as WSTRING, or empty string if not found
WSTRING ExtractCulture(const WSTRING& str);

// Extracts public key token from assembly reference string (e.g., "PublicKeyToken=abcd1234...")
// Returns true if public key was found and parsed successfully
bool ExtractPublicKeyToken(const WSTRING& str, unsigned char* data, const int length);

// Checks if a string matches a secrets pattern (for filtering sensitive environment variables)
// Works with narrow strings (UTF-8)
bool MatchesSecretsPattern(const std::string& str);

} // namespace trace

#endif // OTEL_CLR_PROFILER_REGEX_UTILS_H_
