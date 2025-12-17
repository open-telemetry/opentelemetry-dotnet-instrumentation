// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Mvc;

namespace Examples.AspNetCoreMvc.Controllers;

[Route("api")]
public class ApiController : ControllerBase
{
    [HttpGet]
    [Route("delay/{seconds}")]
    public ActionResult Delay(int seconds)
    {
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        return Ok(seconds);
    }

    [HttpGet]
    [Route("delay-async/{seconds}")]
    public async Task<ActionResult> DelayAsync(int seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        return Ok(seconds);
    }
}
