using Microsoft.AspNetCore.Mvc;

namespace TechNova_IT_Solutions.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Redirect to login page
            return RedirectToAction("Login", "Account");
        }
    }
}
