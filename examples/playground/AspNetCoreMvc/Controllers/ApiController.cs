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
        var results = new System.Collections.Generic.List<string>();
        // results.Add(_businessLogic.ProcessBusinessOperation());
        var operationName = "Sample Operation".ToCharArray().AsSpan();
        results.Add(_businessLogic.ProcessBusinessOperation(operationName));
        // results.Add(_businessLogic.ProcessBusinessOperation(42, DateTime.Now));
        // results.Add(_businessLogic.ProcessBusinessOperation("API Call", new Uri("https://example.com"), DayOfWeek.Monday));
        // results.Add(_businessLogic.ProcessBusinessOperation(99.95, 1, new[] { "important", "urgent" }, s => s.Length > 3));
        // var timeSpan = TimeSpan.FromMinutes(5);
        // var items = new System.Collections.Generic.List<string> { "item1", "item2" };
        // var comparable = "comparable";
        // var metadata = (Name: "MetadataName", Value: 123);
        // var data = new byte[] { 1, 2, 3, 4, 5 };
        // results.Add(_businessLogic.ProcessBusinessOperation(timeSpan, items, comparable, metadata, data));
        // var mappings = new System.Collections.Generic.Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } };
        // var asyncResult = Task.FromResult("async result");
        // var lazyObject = new Lazy<object>(() => new object());
        // var buffer = "buffer";
        // var cancellationToken = CancellationToken.None;
        // var progress = new Progress<int>();
        // results.Add(_businessLogic.ProcessBusinessOperation(mappings, asyncResult, lazyObject, buffer, cancellationToken, progress));
        // var guid = Guid.NewGuid();
        // var type = typeof(string);
        // var stream = new System.IO.MemoryStream();
        // var exception = new Exception("Test exception");
        // var kvp = new System.Collections.Generic.KeyValuePair<string, object>("key", "value");
        // Action<string> logger = s => Console.WriteLine(s);
        // var uniqueValues = new System.Collections.Generic.HashSet<string> { "unique1", "unique2" };
        // results.Add(_businessLogic.ProcessBusinessOperation(guid, type, stream, exception, kvp, logger, uniqueValues));
        // var instance = new object();
        // Delegate callback = new Func<bool>(() => true);
        // var weakRef = new WeakReference(new object());
        // Predicate<string> filter = s => true;
        // var memory = new ReadOnlyMemory<byte>(new byte[] { 9, 8, 7 });
        // var headers = new System.Collections.Generic.Dictionary<string, string> { { "header1", "value1" } };
        // var formatProvider = System.Globalization.CultureInfo.InvariantCulture;
        // var methodInfo = typeof(ApiController).GetMethod("BusinessProcess")!;
        // results.Add(_businessLogic.ProcessBusinessOperation(instance, callback, weakRef, filter, memory, headers, formatProvider, methodInfo));
        // results.Add("DONE");
        return Ok(string.Join("\n", results));
    }
}
