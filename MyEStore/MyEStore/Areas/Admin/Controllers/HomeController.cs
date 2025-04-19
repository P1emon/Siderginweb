using Microsoft.AspNetCore.Mvc;

namespace MyEStore.Areas.Admin.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
