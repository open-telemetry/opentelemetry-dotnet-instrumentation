using System;

namespace Samples.StartupHook.AspNetCoreMvc31.Models;

public class ErrorViewModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}