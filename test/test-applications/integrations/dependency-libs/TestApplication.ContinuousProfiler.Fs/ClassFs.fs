// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.ContinuousProfiler.Fs

open System;
open System.Threading;

module ClassFs =
    let methodFs testParam =
        Console.WriteLine("Thread.Sleep - starting " + testParam)
        Thread.Sleep(TimeSpan.FromSeconds(5.0)) // Give a chance for the continuous profiler to collect a profile.
        Console.WriteLine("Thread.Sleep - finished")
