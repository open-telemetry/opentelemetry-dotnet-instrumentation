/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_ASCII_STRING_UTILS_H_
#define OTEL_CLR_PROFILER_ASCII_STRING_UTILS_H_

#include <algorithm>
#include <string_view>

namespace trace
{

// This works on both UTF-8 bytes and platform-specific WCHAR code units
// without depending on the host process locale or WCHAR width.
template <typename T>
constexpr T ToAsciiLower(const T value) noexcept
{
    return value >= static_cast<T>('A') && value <= static_cast<T>('Z')
               ? static_cast<T>(value - static_cast<T>('A') + static_cast<T>('a'))
               : value;
}

template <typename T>
bool EqualsIgnoreAsciiCase(const std::basic_string_view<T> left, const std::basic_string_view<T> right)
{
    return left.size() == right.size() && std::equal(left.begin(), left.end(), right.begin(),
                                                     [](const T left_char, const T right_char)
                                                     { return ToAsciiLower(left_char) == ToAsciiLower(right_char); });
}

} // namespace trace

#endif // OTEL_CLR_PROFILER_ASCII_STRING_UTILS_H_
