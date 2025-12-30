// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.AspNetCore.Mvc;

namespace TestApplication.Logs.Controllers;

[ApiController]
[Route("[controller]")]
#pragma warning disable CA1515 // Consider making public types internal
public class TestController : ControllerBase
#pragma warning restore CA1515 // Consider making public types internal
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public void Get()
    {
        _logger.LogInformationFromTestApp();
    }
}
