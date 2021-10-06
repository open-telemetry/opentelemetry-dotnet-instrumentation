using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFRAMEWORK
using System.Web.Routing;
#endif
#if !NETFRAMEWORK
using Microsoft.AspNetCore.Routing;
#endif

namespace Datadog.Trace.Util.Http
{
    internal static partial class HttpRequestExtensions
    {
        private static object ConvertRouteValueDictionary(RouteValueDictionary routeDataDict)
        {
            return routeDataDict.ToDictionary(
                c => c.Key,
                c =>
                    c.Value switch
                    {
                        List<RouteData> routeDataList => ConvertRouteValueList(routeDataList),
                        _ => c.Value?.ToString()
                    });
        }

        private static object ConvertRouteValueList(List<RouteData> routeDataList)
        {
            return routeDataList.Select(x => ConvertRouteValueDictionary(x.Values)).ToList();
        }
    }
}
