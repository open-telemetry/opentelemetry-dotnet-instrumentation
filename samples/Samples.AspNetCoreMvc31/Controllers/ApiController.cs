// <copyright file="ApiController.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Samples.AspNetCoreMvc.Controllers;

[Route("api")]
public class ApiController : ControllerBase
{
    private const string CorrelationIdentifierHeaderName = "sample.correlation.identifier";

    [HttpGet]
    [Route("delay/{seconds}")]
    public ActionResult Delay(int seconds)
    {
        Thread.Sleep(TimeSpan.FromSeconds(seconds));
        AddCorrelationIdentifierToResponse();
        return Ok(seconds);
    }

    [HttpGet]
    [Route("delay-async/{seconds}")]
    public async Task<ActionResult> DelayAsync(int seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        AddCorrelationIdentifierToResponse();
        return Ok(seconds);
    }

    private void AddCorrelationIdentifierToResponse()
    {
        if (Request.Headers.ContainsKey(CorrelationIdentifierHeaderName))
        {
            Response.Headers.Add(CorrelationIdentifierHeaderName, Request.Headers[CorrelationIdentifierHeaderName]);
        }
    }
}