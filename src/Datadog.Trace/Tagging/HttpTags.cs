// <copyright file="HttpTags.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace.Tagging
{
    internal abstract class HttpTags : InstrumentationTags, IHasStatusCode
    {
        public override string SpanKind => SpanKinds.Client;

        public string InstrumentationName { get; set; }

        public string HttpMethod { get; set; }

        public string HttpUrl { get; set; }

        public string HttpClientHandlerType { get; set; }

        public string HttpStatusCode { get; set; }
    }
}