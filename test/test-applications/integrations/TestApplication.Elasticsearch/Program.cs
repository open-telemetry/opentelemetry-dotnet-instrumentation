// <copyright file="Program.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
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
// </copyright>

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
