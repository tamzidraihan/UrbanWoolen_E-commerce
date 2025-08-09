using Microsoft.AspNetCore.Mvc;

namespace UrbanWoolen.Controllers
{
    public class AdminController : Controller
    {
        // GET: /Admin/
        public IActionResult Index()
        {
            return View();
        }
    }
}
