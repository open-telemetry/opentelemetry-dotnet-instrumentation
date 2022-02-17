#ifndef OTEL_CLR_PROFILER_MACROS_H_
#define OTEL_CLR_PROFILER_MACROS_H_

#include <corhlpr.h>
#include <fstream>

#define RETURN_IF_FAILED(EXPR) \
  do {                         \
    hr = (EXPR);               \
    if (FAILED(hr)) {          \
      return (hr);             \
    }                          \
  } while (0)

#define IfFalseRetFAIL(EXPR)            \
  do {                                  \
    if ((EXPR) == false) return E_FAIL; \
  } while (0)

#endif  // OTEL_CLR_PROFILER_MACROS_H_
