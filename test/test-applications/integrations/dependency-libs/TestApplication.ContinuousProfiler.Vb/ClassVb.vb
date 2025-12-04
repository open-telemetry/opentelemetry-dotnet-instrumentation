' Copyright The OpenTelemetry Authors
' SPDX-License-Identifier: Apache-2.0
Imports System.Runtime.CompilerServices
Imports TestApplication.ContinuousProfiler.Fs

Public Class ClassVb
    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub MethodVb(testParam As String)
        ClassFs.methodFs(testParam)
    End Sub
End Class

