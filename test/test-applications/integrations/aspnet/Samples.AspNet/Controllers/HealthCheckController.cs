using System.Web.Mvc;

namespace Samples.AspNet.Controllers;

public class HealtCheckController : Controller
{
    public ActionResult Index()
    {
        return new HttpStatusCodeResult(200);
    }
}
