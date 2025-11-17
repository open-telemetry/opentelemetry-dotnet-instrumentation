/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_FILE_UTILS_H_
#define OTEL_CLR_PROFILER_FILE_UTILS_H_

#include <type_traits>

#include "string_utils.h"

#ifdef _WIN32
#define PATH_TO_WSTRING(path_expr) (path_expr).wstring()
#define ENV_VAR_PATH_SEPARATOR WStr(';')
#define ENV_VAR_PATH_SEPARATOR_STR WStr(";")
#else
#define PATH_TO_WSTRING(path_expr) (path_expr).u16string()
#define ENV_VAR_PATH_SEPARATOR WStr(':')
#define ENV_VAR_PATH_SEPARATOR_STR WStr(":")
#endif

#endif // OTEL_CLR_PROFILER_FILE_UTILS_H_
