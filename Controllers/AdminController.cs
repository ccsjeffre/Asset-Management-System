
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
                .Include(b => b.Hardware)
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
                .Include(b => b.Hardware)
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

            if (request.Hardware == null)
            {
                TempData["ErrorMessage"] = "Associated hardware not found for this request. Approval aborted.";
                return RedirectToAction("Approval");
            }

            try
            {
                request.BorrowStatus = "Approved";
                request.ApprovedBy = "LEIF JAY B. DE SAGUN, PhD";

                // Update hardware status
                var hardware = await context.Hardwares
                    .FirstOrDefaultAsync(h => h.HardId == request.Hardware.HardId);

                if (hardware != null)
                {
                    hardware.HardStatus = "On Borrowed";
                }

                // Update inventory based on HardType
                var inventory = await context.Inventorys
                    .FirstOrDefaultAsync(i => i.HardType == request.Hardware.HardType);

                if (inventory != null && inventory.AvailableQuantity >= 1)
                {
                    inventory.AvailableQuantity--;
                    inventory.BorrowedQuantity++;
                    inventory.TotalQuantity = inventory.AvailableQuantity
                                            + inventory.BorrowedQuantity
                                            + inventory.DeployedQuantity
                                            + inventory.NonFunctionalQuantity;
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
                .Include(b => b.Hardware)
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