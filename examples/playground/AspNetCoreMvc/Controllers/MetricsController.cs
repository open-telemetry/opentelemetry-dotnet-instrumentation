// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Examples.AspNetCoreMvc.Controllers;

[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private static readonly Meter MyMeter = new("Examples.AspNetCoreMvc", "1.0");
    private static readonly Counter<long> MyFruitCounter = MyMeter.CreateCounter<long>("MyFruitCounter");

    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        MyFruitCounter.Add(1, new("name", "apple"), new("color", "red"));
        MyFruitCounter.Add(2, new("name", "lemon"), new("color", "yellow"));
        MyFruitCounter.Add(1, new("name", "lemon"), new("color", "yellow"));
        MyFruitCounter.Add(2, new("name", "apple"), new("color", "green"));
        MyFruitCounter.Add(5, new("name", "apple"), new("color", "red"));
        MyFruitCounter.Add(4, new("name", "lemon"), new("color", "yellow"));
        return Ok();
    }
}
