using System.Threading.Tasks;
using OldReference;

namespace BindingRedirect;

public class Program
{
    public static async Task Main(string[] args)
    {
        await InstrumentedHttpCall.GetAsync("https://www.google.com");
    }
}