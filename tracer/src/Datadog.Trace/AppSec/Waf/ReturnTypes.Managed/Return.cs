using System.Linq;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.Waf.ReturnTypes.Managed
{
    internal class Return
    {
        internal bool Blocked { get; private set; }

        internal ResultData ResultData { get; set; }

        internal static Return From(IResult wafReturn)
        {
            return new Return
            {
                ResultData = JsonConvert.DeserializeObject<ResultData[]>(wafReturn.Data).FirstOrDefault(),
                Blocked = wafReturn.ReturnCode == ReturnCode.Block
            };
        }
    }
}
