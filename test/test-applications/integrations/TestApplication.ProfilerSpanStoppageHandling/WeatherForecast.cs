// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ProfilerSpanStoppageHandling;

#pragma warning disable CA1515 // Consider making public types internal
public class WeatherForecast
#pragma warning restore CA1515 // Consider making public types internal
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}
