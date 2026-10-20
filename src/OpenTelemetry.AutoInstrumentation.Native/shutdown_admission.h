// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_SHUTDOWN_ADMISSION_H_
#define OTEL_SHUTDOWN_ADMISSION_H_

namespace continuous_profiler
{
class ShutdownAdmission final
{
public:
    ShutdownAdmission() noexcept;
    ~ShutdownAdmission() noexcept;

    ShutdownAdmission(const ShutdownAdmission&)            = delete;
    ShutdownAdmission& operator=(const ShutdownAdmission&) = delete;

    explicit operator bool() const noexcept
    {
        return admitted_;
    }

private:
    bool admitted_;
};

void CloseShutdownAdmissions() noexcept;
void WaitForShutdownAdmissions() noexcept;
} // namespace continuous_profiler

#endif // OTEL_SHUTDOWN_ADMISSION_H_
