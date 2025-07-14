using Asset_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Asset_Management_System.Controllers
{
    public class HardwaresController : Controller
    {
        private readonly ApplicationDbContext context;

        public HardwaresController(ApplicationDbContext context)
        {
            this.context = context;
        }
        public IActionResult Index(string searchQuery)
        {
            IQueryable<Hardware> hardwares = context.Hardwares;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.ToLower();

                hardwares = hardwares.Where(h =>
                    (h.HardType != null && h.HardType.ToLower().Contains(searchQuery)) ||
                    (h.HardStickerNum != null && h.HardStickerNum.ToLower().Contains(searchQuery))
                );
            }

            var filteredHardwares = hardwares.OrderByDescending(h => h.HardId).ToList();

            ViewBag.CurrentSearchQuery = searchQuery;

            return View(filteredHardwares);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(HardwareDTO hardwareDTO)
        {
            if (!ModelState.IsValid)
            {
                return View(hardwareDTO);
            }

            var typeCodeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Mouse", "MOUSE" },
                { "Monitor", "MON" },
                { "Keyboard", "K" },
                { "System Unit", "SU" },
                { "AVR", "AVR" }
            };

            string rawLocation = hardwareDTO.HardLocation ?? "";
            string rawType = hardwareDTO.HardType ?? "";

            if (!typeCodeMap.ContainsKey(rawType))
            {
                ModelState.AddModelError("HardType", "Unknown hardware type.");
                return View(hardwareDTO);
            }

            string locationCode = rawLocation.Replace("LAB", "L").ToUpper();
            string typeCode = typeCodeMap[rawType];

            int existingCount = await context.Hardwares
                .CountAsync(h => h.HardLocation == hardwareDTO.HardLocation && h.HardType == hardwareDTO.HardType);

            string newStickerNum = $"{locationCode}-{typeCode}{existingCount + 1}";

            var exists = await context.Hardwares
                .AnyAsync(h => h.HardStickerNum == newStickerNum);

            if (exists)
            {
                ModelState.AddModelError("HardStickerNum", "This sticker number already exists.");
                return View(hardwareDTO);
            }

            var hardware = new Hardware
            {
                HardType = hardwareDTO.HardType,
                HardLocation = hardwareDTO.HardLocation,
                HardStickerNum = newStickerNum,
                HardBrand = hardwareDTO.HardBrand,
                HardStatus = "Functional",
                DateAcquisition = hardwareDTO.DateAcquisition
            };

            context.Hardwares.Add(hardware);
            await context.SaveChangesAsync(); // Save first to get HardId

            // Update inventory counts
            var allSameType = await context.Hardwares
                .Where(h => h.HardType == hardware.HardType)
                .ToListAsync();

            int functionalCount = allSameType.Count(h => h.HardStatus == "Functional");
            int borrowedCount = allSameType.Count(h => h.HardStatus == "On Borrowed");
            int notFunctionalCount = allSameType.Count(h =>
                h.HardStatus != "Functional" && h.HardStatus != "On Borrowed");
            int deployedCount = allSameType.Count(h => h.HardStatus == "Deployed");
            int totalCount = functionalCount + borrowedCount + deployedCount + notFunctionalCount;

            var inventory = await context.Inventorys
                .FirstOrDefaultAsync(i => i.HardType == hardware.HardType);

            if (inventory != null)
            {
                inventory.AvailableQuantity = functionalCount;
                inventory.BorrowedQuantity = borrowedCount;
                inventory.NonFunctionalQuantity = notFunctionalCount;
                inventory.DeployedQuantity = deployedCount;
                inventory.TotalQuantity = totalCount;
            }
            else
            {
                var newInventory = new Inventory
                {
                    HardType = hardware.HardType,
                    HardId = hardware.HardId,
                    AvailableQuantity = functionalCount,
                    BorrowedQuantity = borrowedCount,
                    NonFunctionalQuantity = notFunctionalCount,
                    DeployedQuantity = deployedCount,
                    TotalQuantity = totalCount
                };
                context.Inventorys.Add(newInventory);
            }

            await context.SaveChangesAsync();
            return RedirectToAction("Index", "Hardwares");
        }
        public IActionResult Edit(int id)
        {
            var hardware = context.Hardwares.Find(id);
            if (hardware == null)
            {
                return RedirectToAction("Index", "Hardwares");
            }

            var hardwareDTO = new HardwareDTO()
            {
                HardType = hardware.HardType ?? string.Empty,
                HardLocation = hardware.HardLocation ?? string.Empty,
                HardStickerNum = hardware.HardStickerNum ?? string.Empty,
                HardBrand = hardware.HardBrand ?? string.Empty,
                HardStatus = hardware.HardStatus ?? string.Empty,
                DateAcquisition = hardware.DateAcquisition
            };

            ViewData["HardID"] = hardware.HardId;

            return View(hardwareDTO);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, HardwareDTO hardwareDTO)
        {
            var hardware = await context.Hardwares.FindAsync(id);

            if (hardware == null)
            {
                return RedirectToAction("Index", "Hardwares");
            }

            if (!ModelState.IsValid)
            {
                ViewData["HardID"] = hardware.HardId;
                return View(hardwareDTO);
            }

            // Update hardware properties
            hardware.HardType = hardwareDTO.HardType;
            hardware.HardLocation = hardwareDTO.HardLocation;
            hardware.HardStickerNum = hardwareDTO.HardStickerNum;
            hardware.HardBrand = hardwareDTO.HardBrand;
            hardware.HardStatus = hardwareDTO.HardStatus;
            hardware.DateAcquisition = hardwareDTO.DateAcquisition;

            await context.SaveChangesAsync();


            var sameTypeHardware = await context.Hardwares
                .Where(h => h.HardType == hardware.HardType)
                .ToListAsync();

            int functionalCount = sameTypeHardware.Count(h => h.HardStatus == "Functional");
            int borrowedCount = sameTypeHardware.Count(h => h.HardStatus == "On Borrowed");
            int nonFunctionalCount = sameTypeHardware.Count(h =>
                h.HardStatus != "Functional" && h.HardStatus != "On Borrowed");
            int deployedCount = sameTypeHardware.Count(h => h.HardStatus == "Deployed");
            int totalCount = sameTypeHardware.Count;

            var inventory = await context.Inventorys
                .FirstOrDefaultAsync(i => i.HardType == hardware.HardType);

            if (inventory != null)
            {
                inventory.AvailableQuantity = functionalCount;
                inventory.BorrowedQuantity = borrowedCount;
                inventory.NonFunctionalQuantity = nonFunctionalCount;
                inventory.DeployedQuantity = deployedCount;
                inventory.TotalQuantity = totalCount;
            }

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Hardwares");
        }

    }
}
