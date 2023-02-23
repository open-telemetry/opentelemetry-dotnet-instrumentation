//------------------------------------------------------------------------------
// <auto-generated />
// This comment is here to prevent StyleCop from analyzing a file originally from Serilog.
//------------------------------------------------------------------------------

// Copyright 2013-2019 Serilog Contributors
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

// Modified by OpenTelemetry Authors

using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal sealed class FileSink : IDisposable
{
    readonly TextWriter _output;
    readonly FileStream _underlyingStream;
    readonly WriteCountingStream _countingStreamWrapper;
    readonly object _syncRoot = new object();
    readonly long _fileSizeLimitBytes;

    public FileSink(string path, long fileSizeLimitBytes)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (fileSizeLimitBytes < 1)
        {
            throw new ArgumentException("Invalid value provided; file size limit must be at least 1 byte.");
        }
        _fileSizeLimitBytes = fileSizeLimitBytes;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _underlyingStream = System.IO.File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);

        Stream outputStream = _countingStreamWrapper = new WriteCountingStream(_underlyingStream);
        _output = new StreamWriter(outputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public bool Write(string message)
    {
        lock (_syncRoot)
        {
            if (_countingStreamWrapper.CountedLength >= _fileSizeLimitBytes)
            {
                return false;
            }
            _output.Write(message);
            FlushToDisk();
            return true;
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            _output.Dispose();
        }
    }

    public void FlushToDisk()
    {
        _output.Flush();
        _underlyingStream.Flush(true);
    }
}
