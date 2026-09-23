// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class EffectiveConfigSnapshot
{
    internal const int MaxFileCount = 16;
    internal const int MaxFileSize = 512 * 1024;

    private readonly EffectiveConfigFile[] _files;

    private EffectiveConfigSnapshot(EffectiveConfigFile[] files)
    {
        _files = files;
        Files = Array.AsReadOnly(files);
    }

    public IReadOnlyCollection<EffectiveConfigFile> Files { get; }

    public static bool TryCreate(
        IReadOnlyCollection<EffectiveConfigFile>? files,
        [NotNullWhen(true)] out EffectiveConfigSnapshot? snapshot)
    {
        snapshot = null;
        if (files == null)
        {
            return false;
        }

        try
        {
            if (files.Count > MaxFileCount)
            {
                return false;
            }

            var entries = new List<SnapshotEntry>(files.Count);
            var fileNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var file in files)
            {
                if (file == null ||
                    file.ContentType == null ||
                    file.FileName == null ||
                    entries.Count == MaxFileCount)
                {
                    return false;
                }

                var contentLength = file.Content.Length;
                if (contentLength > MaxFileSize)
                {
                    return false;
                }

                if (!fileNames.Add(file.FileName))
                {
                    return false;
                }

                entries.Add(new SnapshotEntry(file.Content.ToArray(), file.ContentType, file.FileName));
            }

            if (entries.Count != files.Count)
            {
                return false;
            }

            entries.Sort((left, right) => StringComparer.Ordinal.Compare(left.FileName, right.FileName));

            var copiedFiles = new EffectiveConfigFile[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                copiedFiles[i] = new EffectiveConfigFile(entry.Content, entry.ContentType, entry.FileName);
            }

            snapshot = new EffectiveConfigSnapshot(copiedFiles);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool HasSameContent(EffectiveConfigSnapshot other)
    {
        if (_files.Length != other._files.Length)
        {
            return false;
        }

        for (var i = 0; i < _files.Length; i++)
        {
            var file = _files[i];
            var otherFile = other._files[i];
            if (!StringComparer.Ordinal.Equals(file.FileName, otherFile.FileName) ||
                !StringComparer.Ordinal.Equals(file.ContentType, otherFile.ContentType) ||
                !file.Content.Span.SequenceEqual(otherFile.Content.Span))
            {
                return false;
            }
        }

        return true;
    }

    private readonly struct SnapshotEntry
    {
        public SnapshotEntry(byte[] content, string contentType, string fileName)
        {
            Content = content;
            ContentType = contentType;
            FileName = fileName;
        }

        public byte[] Content { get; }

        public string ContentType { get; }

        public string FileName { get; }
    }
}
