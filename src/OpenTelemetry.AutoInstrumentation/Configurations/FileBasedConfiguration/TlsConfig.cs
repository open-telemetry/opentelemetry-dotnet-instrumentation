// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class TlsConfig
{
    /// <summary>
    /// Gets or sets the certificate used to verify a server's TLS credentials.
    /// Absolute path to certificate file in PEM format.
    /// If omitted or null, system default certificate verification is used for secure connections.
    /// </summary>
    [YamlMember(Alias = "certificate_file")]
    public string? CertificateFile { get; set; }

    /// <summary>
    /// Gets or sets the mTLS private client key.
    /// Absolute path to client key file in PEM format.
    /// If set, <see cref="ClientCertificateFile"/> must also be set.
    /// If omitted or null, mTLS is not used.
    /// </summary>
    [YamlMember(Alias = "client_key_file")]
    public string? ClientKeyFile { get; set; }

    /// <summary>
    /// Gets or sets the mTLS client certificate.
    /// Absolute path to client certificate file in PEM format.
    /// If set, <see cref="ClientKeyFile"/> must also be set.
    /// If omitted or null, mTLS is not used.
    /// </summary>
    [YamlMember(Alias = "client_certificate_file")]
    public string? ClientCertificateFile { get; set; }
}
