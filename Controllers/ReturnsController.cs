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
                .Include(b => b.BorrowedHardwares)
                    .ThenInclude(bh => bh.Hardware)
                .ToList();

            return View(borrowers);
        }
    }
}
