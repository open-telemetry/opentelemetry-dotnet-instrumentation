// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Examples.AspNetCoreMvc.Logic;
using Examples.AspNetCoreMvc.Messages;
using Microsoft.AspNetCore.Mvc;
using NServiceBus;

namespace Examples.AspNetCoreMvc.Controllers;

[Route("api")]
public class ApiController : ControllerBase
{
    private readonly IMessageSession _messageSession;
    private readonly BusinessLogic _businessLogic;

    public ApiController(IMessageSession messageSession, BusinessLogic businessLogic)
    {
        _messageSession = messageSession;
        _businessLogic = businessLogic;
    }

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

    [HttpGet]
    [Route("send-message")]
    public async Task<ActionResult> SendMessage()
    {
        var command = new TestMessage();
        await _messageSession.Send(command);
        return Ok("Message sent successfully");
    }

    [HttpGet]
    [Route("business-process")]
    public ActionResult BusinessProcess()
    {
        var result = _businessLogic.ProcessBusinessOperation("PROCESSING A BUSINESS OPERATION");
        return Ok(result);
    }
}
