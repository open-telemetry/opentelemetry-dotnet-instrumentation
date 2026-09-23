// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal interface IOpAmpReportingProcessor
{
    void Process(OpAmpReportingRequests requests);
}
