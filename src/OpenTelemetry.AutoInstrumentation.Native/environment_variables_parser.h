/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_PARSER_H_
#define OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_PARSER_H_

#define TrueCondition(EXPR)                      \
    ((EXPR).size() == 4                          \
     && ((EXPR)[0] == L't' || (EXPR)[0] == L'T') \
     && ((EXPR)[1] == L'r' || (EXPR)[1] == L'R') \
     && ((EXPR)[2] == L'u' || (EXPR)[2] == L'U') \
     && ((EXPR)[3] == L'e' || (EXPR)[3] == L'E'))

#define FalseCondition(EXPR)                    \
    ((EXPR).size() == 5                         \
    && ((EXPR)[0] == L'f' || (EXPR)[0] == L'F') \
    && ((EXPR)[1] == L'a' || (EXPR)[1] == L'A') \
    && ((EXPR)[2] == L'l' || (EXPR)[2] == L'L') \
    && ((EXPR)[3] == L's' || (EXPR)[3] == L'S') \
    && ((EXPR)[4] == L'e' || (EXPR)[4] == L'E'))


#endif  // OTEL_CLR_PROFILER_ENVIRONMENT_VARIABLES_PARSER_H_
