using System.Web.Mvc;

namespace Samples.AspNet.Controllers;

public class HealthCheckController : Controller
{
    public ActionResult Index()
    {
        return new HttpStatusCodeResult(200);
    }
}
