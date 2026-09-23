// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class EffectiveConfigSnapshotTests
{
    [Fact]
    public void CanonicalizesAndCopiesConfiguration()
    {
        var mutableContent = new byte[] { 1, 2, 3 };
        var files = new[]
        {
            new EffectiveConfigFile(new byte[] { 4 }, "text/plain", "second"),
            new EffectiveConfigFile(mutableContent, "application/json", "first")
        };

        Assert.True(EffectiveConfigSnapshot.TryCreate(files, out var snapshot));
        mutableContent[0] = 9;
        Assert.True(EffectiveConfigSnapshot.TryCreate(
            [
                new EffectiveConfigFile(new byte[] { 1, 2, 3 }, "application/json", "first"),
                new EffectiveConfigFile(new byte[] { 4 }, "text/plain", "second")
            ],
            out var equivalentSnapshot));

        Assert.Equal(["first", "second"], snapshot!.Files.Select(file => file.FileName));
        Assert.Equal(new byte[] { 1, 2, 3 }, snapshot.Files.First().Content.ToArray());
        Assert.True(snapshot.HasSameContent(equivalentSnapshot!));
    }

    [Fact]
    public void ContentEqualityIncludesAllFileProperties()
    {
        Assert.True(EffectiveConfigSnapshot.TryCreate([CreateFile(1, "file", "text/plain")], out var baseline));
        Assert.True(EffectiveConfigSnapshot.TryCreate([CreateFile(1, "other", "text/plain")], out var otherName));
        Assert.True(EffectiveConfigSnapshot.TryCreate([CreateFile(1, "file", "application/json")], out var otherContentType));
        Assert.True(EffectiveConfigSnapshot.TryCreate(
            [new EffectiveConfigFile(new byte[] { 1 }, "text/plain", "file")],
            out var otherContent));

        Assert.False(baseline!.HasSameContent(otherName!));
        Assert.False(baseline.HasSameContent(otherContentType!));
        Assert.False(baseline.HasSameContent(otherContent!));
    }

    [Fact]
    public void AcceptsMaximumFileCount()
    {
        var files = Enumerable.Range(0, EffectiveConfigSnapshot.MaxFileCount)
            .Select(index => CreateFile(0, index.ToString(CultureInfo.InvariantCulture)))
            .ToArray();

        Assert.True(EffectiveConfigSnapshot.TryCreate(files, out _));
    }

    [Fact]
    public void AcceptsMaximumFileSize()
    {
        Assert.True(EffectiveConfigSnapshot.TryCreate(
            [CreateFile(EffectiveConfigSnapshot.MaxFileSize, "file")],
            out _));
    }

    [Fact]
    public void RejectsTooManyFiles()
    {
        var files = Enumerable.Range(0, EffectiveConfigSnapshot.MaxFileCount + 1)
            .Select(index => CreateFile(0, index.ToString(CultureInfo.InvariantCulture)))
            .ToArray();

        Assert.False(EffectiveConfigSnapshot.TryCreate(files, out _));
    }

    [Fact]
    public void RejectsOversizedFile()
    {
        Assert.False(EffectiveConfigSnapshot.TryCreate(
            [CreateFile(EffectiveConfigSnapshot.MaxFileSize + 1, "file")],
            out _));
    }

    [Fact]
    public void EnforcesFilenameUniquenessOrdinally()
    {
        Assert.False(EffectiveConfigSnapshot.TryCreate([CreateFile(0, "same"), CreateFile(0, "same")], out _));
        Assert.False(EffectiveConfigSnapshot.TryCreate([CreateFile(0, string.Empty), CreateFile(0, string.Empty)], out _));
        Assert.True(EffectiveConfigSnapshot.TryCreate([CreateFile(0, "name"), CreateFile(0, "NAME")], out _));
    }

    [Fact]
    public void AcceptsEmptyFilename()
    {
        Assert.True(EffectiveConfigSnapshot.TryCreate([CreateFile(0, string.Empty)], out _));
        Assert.True(EffectiveConfigSnapshot.TryCreate([CreateFile(0, string.Empty), CreateFile(0, "named")], out _));
    }

    [Fact]
    public void RejectsInvalidProviderOutput()
    {
        Assert.False(EffectiveConfigSnapshot.TryCreate(null, out _));
        Assert.False(EffectiveConfigSnapshot.TryCreate([null!], out _));
        Assert.False(EffectiveConfigSnapshot.TryCreate(new ThrowingCollection(), out _));
    }

    private static EffectiveConfigFile CreateFile(int size, string fileName, string contentType = "application/json")
    {
        return new EffectiveConfigFile(new byte[size], contentType, fileName);
    }

    private sealed class ThrowingCollection : IReadOnlyCollection<EffectiveConfigFile>
    {
        public int Count => 1;

        public IEnumerator<EffectiveConfigFile> GetEnumerator()
        {
            throw new InvalidOperationException("Enumeration failed.");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
