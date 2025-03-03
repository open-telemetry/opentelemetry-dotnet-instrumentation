// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace Examples.AspNetCoreMvc.Logic;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Provides business logic functionality for the application.
/// </summary>
public class BusinessLogic
{
    public string ProcessBusinessOperation(string operationName)
    {
        return operationName;
    }
}
