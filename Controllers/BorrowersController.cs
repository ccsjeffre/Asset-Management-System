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
            // Get list of hardware IDs that are currently borrowed (not returned yet)
            var borrowedHardwareIds = await context.BorrowedHardwares
                .Include(bh => bh.Borrower)
                .Where(bh => bh.Borrower != null && bh.Borrower.ReturnOn == null)
                .Select(bh => bh.HardId)
                .ToListAsync();

            // Filter for hardwares with status Available or Functional and not currently borrowed
            var selectableHardwares = await context.Hardwares
                .Where(h => (h.HardStatus == "Available" || h.HardStatus == "Functional")
                            && !borrowedHardwareIds.Contains(h.HardId))
                .OrderBy(h => h.HardStickerNum)
                .Select(h => new
                {
                    h.HardId,
                    Display = h.HardType + " - " + h.HardStickerNum
                })
                .ToListAsync();

            // Populate dropdowns with combined label
            ViewBag.HardwareTypes = new MultiSelectList(selectableHardwares, "HardId", "Display");
            ViewBag.StickerNumbers = new SelectList(selectableHardwares, "HardId", "Display");
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

            if (borrowerDTO.HardIds == null || !borrowerDTO.HardIds.Any())
            {
                ModelState.AddModelError(string.Empty, "Please select at least one hardware.");
                await PopulateHardwareDropdowns();
                return View(borrowerDTO);
            }

            var borrower = new Borrower
            {
                BorrowersName = borrowerDTO.BorrowersName,
                Department = borrowerDTO.Department,
                BorrowPurpose = borrowerDTO.BorrowPurpose,
                BorrowStatus = borrowerDTO.BorrowStatus,
                BorrowedOn = DateTime.Today,
                ReturnOn = borrowerDTO.ReturnOn,
                ApprovedBy = "LEIF JAY B. DE SAGUN, PhD",
                ReleasedBy = borrowerDTO.ReleasedBy,
                ReceivedBy = borrowerDTO.ReceivedBy,
                BorrowedHardwares = borrowerDTO.HardIds.Select(id => new BorrowedHardware
                {
                    HardId = id
                }).ToList()
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
                .Include(b => b.BorrowedHardwares)
                    .ThenInclude(bh => bh.Hardware)
                .OrderByDescending(b => b.BorrowersId)
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
        public async Task<IActionResult> ReturnHardware(int id, string ReceivedBy, DateTime ReturnOn, List<string> ReturnedStatuses)
        {
            var borrower = await context.Borrowers
                .Include(b => b.BorrowedHardwares)
                .FirstOrDefaultAsync(b => b.BorrowersId == id);

            if (borrower == null)
                return NotFound();

            borrower.ReceivedBy = ReceivedBy;
            borrower.ReturnOn = ReturnOn;
            borrower.BorrowStatus = "Returned";

            // Assign the status to each hardware item
            for (int i = 0; i < borrower.BorrowedHardwares.Count; i++)
            {
                var bh = borrower.BorrowedHardwares.ElementAt(i);
                var hardware = await context.Hardwares.FindAsync(bh.HardId);
                if (hardware != null && i < ReturnedStatuses.Count)
                {
                    hardware.HardStatus = ReturnedStatuses[i]; // Use selected value
                }
            }

            await context.SaveChangesAsync();
            return RedirectToAction("BorrowerRequestsList", "Returns");
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
        public async Task<IActionResult> Returning()
        {
            var borrowers = await context.Borrowers
                .Include(b => b.BorrowedHardwares)
                    .ThenInclude(bh => bh.Hardware)
                .Where(b => b.BorrowStatus == "Approved")
                .ToListAsync();

            return View(borrowers);
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
