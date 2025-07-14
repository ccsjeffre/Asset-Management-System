using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asset_Management_System.Controllers
{
    public class ReturnsController : Controller
    {
        private ApplicationDbContext context;

        public ReturnsController(ApplicationDbContext context)
        {
            this.context = context;
        }
        public IActionResult BorrowerRequestsList()
        {
            var borrowers = context.Borrowers
                .Include(b => b.Hardware)
                .ToList();

            return View(borrowers);
        }

    }
}
