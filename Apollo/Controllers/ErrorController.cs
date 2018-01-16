namespace Apollo.Controllers
{
    using System.Web.Mvc;

    public class ErrorController : Controller
    {
        // GET: Shared
        public ActionResult Index()
        {
            return View("Error");
        }
    }
}