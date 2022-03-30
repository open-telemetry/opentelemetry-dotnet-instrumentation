using System.Threading.Tasks;
using OldReference;

namespace CoreAppOldReference
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await InstrumentedHttpCall.GetAsync("https://www.google.com");
            await InstrumentedHttpCall.GetAsync("http://127.0.0.1:8080/api/mongo");
            await InstrumentedHttpCall.GetAsync("http://127.0.0.1:8080/api/redis");
        }
    }
}
