using Asset_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Asset_Management_System.Controllers
{
    public class BorrowersController : Controller
    {
        private readonly ApplicationDbContext context;

        public BorrowersController(ApplicationDbContext context)
        {
            this.context = context;
        }

        private async Task PopulateHardwareDropdowns()
        {
            var borrowedHardwareIds = await context.Borrowers
                .Where(b => b.ReturnOn == null)
                .Select(b => b.HardId)
                .ToListAsync();

            var functionalHardwares = await context.Hardwares
                .Where(h => h.HardStatus == "Functional")
                .OrderBy(h => h.HardStickerNum)
                .ToListAsync();

            ViewBag.HardwareTypes = new SelectList(functionalHardwares, "HardId", "HardType");
            ViewBag.StickerNumbers = new SelectList(functionalHardwares, "HardId", "HardStickerNum");
        }

        [HttpGet]
        public async Task<IActionResult> RequestHardware()
        {
            var dto = new BorrowerDTO();
            await PopulateHardwareDropdowns();
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> RequestHardware(BorrowerDTO borrowerDTO)
        {
            if (!ModelState.IsValid)
            {
                await PopulateHardwareDropdowns();
                return View(borrowerDTO);
            }

            var hardware = await context.Hardwares
                .FirstOrDefaultAsync(h => h.HardId == borrowerDTO.HardId);

            if (hardware == null)
            {
                ModelState.AddModelError("HardId", "Selected hardware is invalid.");
                await PopulateHardwareDropdowns();
                return View(borrowerDTO);
            }

            // Check if the hardware is already requested and pending
            var existingPendingRequest = await context.Borrowers
                .AnyAsync(b => b.HardId == borrowerDTO.HardId && b.BorrowStatus == "Pending");

            if (existingPendingRequest)
            {
                ModelState.AddModelError(string.Empty, "This hardware is already pending in another request.");
                await PopulateHardwareDropdowns();
                return View(borrowerDTO);
            }

            var borrower = new Borrower
            {
                HardId = borrowerDTO.HardId,
                BorrowersName = borrowerDTO.BorrowersName,
                Department = borrowerDTO.Department,
                BorrowPurpose = borrowerDTO.BorrowPurpose,
                BorrowStatus = borrowerDTO.BorrowStatus,
                BorrowedOn = DateTime.Today,
                ReturnOn = borrowerDTO.ReturnOn,
                ApprovedBy = "LEIF JAY B. DE SAGUN, PhD",
                ReleasedBy = borrowerDTO.ReleasedBy,
                ReceivedBy = borrowerDTO.ReceivedBy
            };

            context.Borrowers.Add(borrower);
            await context.SaveChangesAsync();

            return RedirectToAction("MyRequests");
        }

        public async Task<IActionResult> GetInventoryStatusPartial()
        {
            var inventories = await context.Inventorys.ToListAsync();

            var dtoList = inventories.Select(i => new InventoryDTO
            {
                HardType = i.HardType,
                AvailableQuantity = i.AvailableQuantity,
                NonFunctionalQuantity = i.NonFunctionalQuantity,
                BorrowedQuantity = i.BorrowedQuantity,
                DeployedQuantity = i.DeployedQuantity,
                TotalQuantity = i.TotalQuantity
            }).ToList();

            return PartialView("_InventoryTablePartial", dtoList);
        }

        public async Task<IActionResult> MyRequests()
        {
            var borrowers = await context.Borrowers
                .Include(b => b.Hardware)
                .OrderByDescending(h => h.BorrowersId)
                .ToListAsync();

            return View(borrowers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateReleasedBy(int id, string ReleasedBy)
        {
            var borrower = context.Borrowers.Find(id);
            if (borrower != null)
            {
                borrower.ReleasedBy = ReleasedBy;
                context.SaveChanges();
            }
            return RedirectToAction("MyRequests");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnHardware(int id)
        {
            var request = await context.Borrowers
                .Include(b => b.Hardware)
                .FirstOrDefaultAsync(b => b.BorrowersId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Borrow request not found.";
                return RedirectToAction("MyRequests");
            }

            if (request.BorrowStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Only approved requests can be returned.";
                return RedirectToAction("MyRequests");
            }

            try
            {
                request.BorrowStatus = "Returned";

                if (request.Hardware == null)
                {
                    TempData["ErrorMessage"] = "Associated hardware not found.";
                    return RedirectToAction("MyRequests");
                }


                request.Hardware.HardStatus = "Avaibale";

                //Update inventory
                var inventory = await context.Inventorys
                    .FirstOrDefaultAsync(i => i.HardType == request.Hardware.HardType);

                if (inventory != null)
                {
                    inventory.BorrowedQuantity--;
                    inventory.AvailableQuantity++;
                }

                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hardware returned successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error while returning hardware: " + ex.Message;
            }

            return RedirectToAction("MyRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateReceivedBy(int id, string ReceivedBy)
        {
            var borrower = context.Borrowers.Find(id);
            if (borrower != null)
            {
                borrower.ReceivedBy = ReceivedBy;
                context.SaveChanges();
            }
            return RedirectToAction("MyRequests");
        }

        public async Task<IActionResult> InventoryStatus()
        {
            var inventoryData = await context.Hardwares
                .GroupBy(h => h.HardType)
                .Select(group => new InventoryDTO
                {
                    HardType = group.Key,
                    AvailableQuantity = group.Count(h => h.HardStatus == "Functional"),
                    BorrowedQuantity = group.Count(h => h.HardStatus == "On Borrowed"),
                    NonFunctionalQuantity = group.Count(h => h.HardStatus != "Functional" && h.HardStatus != "On Borrowed"),
                    DeployedQuantity = group.Count(h => h.HardStatus == "Deployed"),
                    TotalQuantity = group.Count()
                })
                .ToListAsync();

            return View(inventoryData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelRequest(int id)
        {
            var request = context.Borrowers.Find(id);
            if (request == null)
            {
                return NotFound();
            }

            context.Borrowers.Remove(request);
            context.SaveChanges();

            return RedirectToAction("MyRequests");
        }
    }
}
