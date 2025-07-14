using Microsoft.AspNetCore.Mvc;

namespace Asset_Management_System.Controllers
{
    public class LandingPageController : Controller
    {
        public IActionResult Dashboard() => View();
    }
}
