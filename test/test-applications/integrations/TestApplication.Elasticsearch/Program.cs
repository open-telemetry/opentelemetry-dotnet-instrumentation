// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using TestApplication.Shared;

ConsoleHelper.WriteSplashScreen(args);

// Real elasticsearch connection is not needed. Expected Span is created even without working environment.
const string fakeUsername = "fakeUsername";
const string fakePassword = "fakePassword";
const string fakeCertificateFingerprint = "cafd5a42cdb5df7ec3bd1cc19d526e284ccc82129da412b5794de1ab0eebeff2";
var fakeUri = new Uri("https://fakeendpoint:9200");

var settings = new ElasticsearchClientSettings(fakeUri)
    .CertificateFingerprint(fakeCertificateFingerprint)
    .Authentication(new BasicAuthentication(fakeUsername, fakePassword));

var client = new ElasticsearchClient(settings);

try
{
    await client.SearchAsync<TestObject>(s =>
        s.Index("test-index").From(0).Size(10).Query(q => q.Term(t => t.Id, 1)));
}
catch (UnexpectedTransportException)
{
    // ignore this exception as it does not impact creating activity
}
