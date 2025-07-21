
using Asset_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asset_Management_System.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext context;

        public AdminController(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<IActionResult> Approval()
        {
            var requests = await context.Borrowers
                .Include(b => b.BorrowedHardwares!)
                    .ThenInclude(bh => bh.Hardware)
                .Where(b => b.BorrowStatus == "Pending")
                .OrderByDescending(b => b.BorrowedOn)
                .ToListAsync();

            return View(requests);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await context.Borrowers
                .Include(b => b.BorrowedHardwares!)
                    .ThenInclude(bh => bh.Hardware)
                .FirstOrDefaultAsync(b => b.BorrowersId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Borrow request not found.";
                return RedirectToAction("Approval");
            }

            if (request.BorrowStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Request is not in 'Pending' status and cannot be approved.";
                return RedirectToAction("Approval");
            }

            if (request.BorrowedHardwares == null || !request.BorrowedHardwares.Any())
            {
                TempData["ErrorMessage"] = "No hardware associated with this request.";
                return RedirectToAction("Approval");
            }

            try
            {
                request.BorrowStatus = "Approved";
                request.ApprovedBy = "LEIF JAY B. DE SAGUN, PhD";

                foreach (var borrowed in request.BorrowedHardwares)
                {
                    var hardware = borrowed.Hardware;
                    if (hardware != null)
                    {
                        hardware.HardStatus = "On Borrowed";

                        // Update inventory
                        var inventory = await context.Inventorys
                            .FirstOrDefaultAsync(i => i.HardType == hardware.HardType);

                        if (inventory != null && inventory.AvailableQuantity >= 1)
                        {
                            inventory.AvailableQuantity--;
                            inventory.BorrowedQuantity++;
                            inventory.TotalQuantity = inventory.AvailableQuantity
                                                    + inventory.BorrowedQuantity
                                                    + inventory.DeployedQuantity
                                                    + inventory.NonFunctionalQuantity;
                        }
                    }
                }

                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Request approved successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("Approval");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await context.Borrowers
                .Include(b => b.BorrowedHardwares) // optional; no need to access Hardware here
                .FirstOrDefaultAsync(b => b.BorrowersId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Borrow request not found.";
                return RedirectToAction("Approval");
            }

            if (request.BorrowStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Request is not in 'Pending' status and cannot be rejected.";
                return RedirectToAction("Approval");
            }

            try
            {
                request.BorrowStatus = "Rejected";
                request.ApprovedBy = "LEIF JAY B. DE SAGUN, PhD";

                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Request rejected successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the request: " + ex.Message;
            }

            return RedirectToAction("Approval");
        }

    }
}