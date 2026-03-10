// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;

namespace TestApplication.ProfilerSpanStoppageHandling.Controllers;

[ApiController]
[Route("[controller]")]
#pragma warning disable CA1515 // Consider making public types internal
public class WeatherForecastController : ControllerBase
#pragma warning restore CA1515 // Consider making public types internal
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private readonly TestDependency _dependency;

    public WeatherForecastController(TestDependency dependency)
    {
        _dependency = dependency;
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        Thread.Sleep(1000);
        return [.. Enumerable.Range(1, 5).Select(static index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = RandomNumberGenerator.GetInt32(-20, 55),
            Summary = Summaries[RandomNumberGenerator.GetInt32(Summaries.Length)]
        })];
    }
}
