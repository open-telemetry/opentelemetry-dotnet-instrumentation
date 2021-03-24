using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;

namespace Datadog.Trace.TestHelpers
{
    public abstract class HeadersCollectionTestBase
    {
        public static IEnumerable<object[]> GetHeaderCollectionImplementations()
        {
            yield return new object[] { WebRequest.CreateHttp("http://localhost").Headers.Wrap() };
            yield return new object[] { new NameValueCollection().Wrap() };
            yield return new object[] { new DictionaryHeadersCollection() };
        }

        public static IEnumerable<object[]> GetHeadersInvalidIdsCartesianProduct()
        {
            return from header in GetHeaderCollectionImplementations().SelectMany(i => i)
                   from invalidId in HeadersCollectionTestHelpers.GetInvalidIds().SelectMany(i => i)
                   select new[] { header, invalidId };
        }

        public static IEnumerable<object[]> GetHeadersInvalidSamplingPrioritiesCartesianProduct()
        {
            return from header in GetHeaderCollectionImplementations().SelectMany(i => i)
                   from invalidSamplingPriority in HeadersCollectionTestHelpers.GetInvalidSamplingPriorities().SelectMany(i => i)
                   select new[] { header, invalidSamplingPriority };
        }
    }
}
