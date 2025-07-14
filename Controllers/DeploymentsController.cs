using Asset_Management_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace Asset_Management_System.Controllers
{
    public class DeploymentsController : Controller
    {
        private ApplicationDbContext context;

        public DeploymentsController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<IActionResult> DeploymentRequestList()
        {
            var deployments = await context.Deployments
                                            .Include(d => d.Hardware)
                                            .ToListAsync();

            return View(deployments);
        }

        [HttpGet]
        public async Task<IActionResult> RequestDeployment()
        {
            await PopulateHardwareDropdowns();
            return View(new DeploymentDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestDeployment(DeploymentDTO deploymentDTO)
        {
            if (deploymentDTO.HardId == 0)
            {
                ModelState.AddModelError("HardId", "Please select a hardware item.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateHardwareDropdowns();
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return View(deploymentDTO);
            }

            var selectedHardware = await context.Hardwares.FindAsync(deploymentDTO.HardId);
            if (selectedHardware == null)
            {
                ModelState.AddModelError(string.Empty, "Selected hardware not found or is no longer functional.");
                await PopulateHardwareDropdowns();
                TempData["ErrorMessage"] = "Selected hardware not found or is no longer functional.";
                return View(deploymentDTO);
            }

            if (selectedHardware.HardStatus != "Functional")
            {
                ModelState.AddModelError(string.Empty, $"Selected hardware is not functional. Current status: {selectedHardware.HardStatus}");
                await PopulateHardwareDropdowns();
                TempData["ErrorMessage"] = $"Selected hardware is not functional. Current status: {selectedHardware.HardStatus}";
                return View(deploymentDTO);
            }

            try
            {
                var deployment = new Deployment
                {
                    HardId = deploymentDTO.HardId,
                    DeployHardware = selectedHardware.HardType,
                    DeployRequestorName = deploymentDTO.DeployRequestorName,
                    DeployArea = deploymentDTO.DeployArea,
                    DeployPurpose = deploymentDTO.DeployPurpose,
                    DeployDate = deploymentDTO.DeployDate,
                    DeployStatus = deploymentDTO.DeployStatus,
                    ReleasedBy = deploymentDTO.ReleasedBy,
                    ReceivedBy = deploymentDTO.ReceivedBy
                };


                //ADD & UPDATE
                context.Deployments.Add(deployment);
                selectedHardware.HardStatus = "Pending";
                context.Update(selectedHardware);
                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Deployment request submitted successfully!";
                return RedirectToAction("DeploymentRequestList", "Deployments");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error submitting deployment: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);

                TempData["ErrorMessage"] = "An unexpected error occurred while submitting the deployment request.";
                await PopulateHardwareDropdowns();
                return View(deploymentDTO);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await context.Deployments
                .Include(b => b.Hardware)
                .FirstOrDefaultAsync(b => b.DeployId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Borrow request not found.";
                return RedirectToAction("DeploymentApproval");
            }

            if (request.DeployStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Request is not in 'Pending' status and cannot be rejected.";
                return RedirectToAction("DeploymentApproval");
            }

            try
            {
                request.DeployStatus = "Rejected";
                request.ApprovedBy = "LEIF JAY B. DE SAGUN, PhD";

                await context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Request rejected successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while rejecting the request: " + ex.Message;
            }

            return RedirectToAction("DeploymentApproval");
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ApproveRequest(int id)
        //{
        //    var request = await context.Deployments
        //        .Include(b => b.Hardware)
        //        .FirstOrDefaultAsync(d => d.DeployId == id);

        //    if (request == null)
        //    {
        //        TempData["ErrorMessage"] = "Borrow request not found.";
        //        return RedirectToAction("DeploymentApproval");
        //    }

        //    if (request.DeployStatus != "Pending")
        //    {
        //        TempData["ErrorMessage"] = "Request is not in 'Pending' status and cannot be approved.";
        //        return RedirectToAction("DeploymentApproval");
        //    }

        //    if (request.Hardware == null)
        //    {
        //        TempData["ErrorMessage"] = "Associated hardware not found for this request. Approval aborted.";
        //        return RedirectToAction("DeploymentApproval");
        //    }

        //    try
        //    {
        //        request.DeployStatus = "Approved";
        //        request.ApprovedBy = "LEIF JAY B. DE SAGUN, PhD";

        //        // Update hardware status
        //        var hardware = await context.Hardwares
        //            .FirstOrDefaultAsync(h => h.HardId == request.Hardware.HardId);

        //        if (hardware != null)
        //        {
        //            hardware.HardStatus = "On Borrowed";
        //        }

        //        // Update inventory based on HardType
        //        var inventory = await context.Inventorys
        //            .FirstOrDefaultAsync(i => i.HardType == request.Hardware.HardType);

        //        if (inventory != null && inventory.AvailableQuantity >= 1)
        //        {
        //            inventory.AvailableQuantity--;
        //            inventory.BorrowedQuantity++;
        //            inventory.TotalQuantity = inventory.AvailableQuantity
        //                                    + inventory.BorrowedQuantity
        //                                    + inventory.NonFunctionalQuantity;
        //        }

        //        await context.SaveChangesAsync();

        //        TempData["SuccessMessage"] = "Request approved successfully!";
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
        //    }

        //    return RedirectToAction("DeploymentApproval");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var request = await context.Deployments
                .Include(d => d.Hardware)
                .FirstOrDefaultAsync(d => d.DeployId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Borrow request not found.";
                return RedirectToAction("DeploymentApproval");
            }

            if (request.DeployStatus != "Pending")
            {
                TempData["ErrorMessage"] = "Request is not in 'Pending' status and cannot be approved.";
                return RedirectToAction("DeploymentApproval");
            }

            var hardware = await context.Hardwares.FindAsync(request.HardId);

            try
            {
                request.DeployStatus = "Approved";
                request.ApprovedBy = "LEIF JAY B. DE SAGUN, PhD";
                if (hardware != null)
                {
                    hardware.HardStatus = "Deployed";
                }
                else
                {
                    TempData["ErrorMessage"] = "Hardware record not found. Cannot update status.";
                    return RedirectToAction("DeploymentApproval");
                }

                // Update Inventory
                var inventory = await context.Inventorys
                    .FirstOrDefaultAsync(i => i.HardType == hardware.HardType);

                if (inventory != null && inventory.AvailableQuantity > 0)
                {
                    inventory.AvailableQuantity--;
                    inventory.DeployedQuantity++;
                    inventory.TotalQuantity = inventory.AvailableQuantity
                                            + inventory.BorrowedQuantity
                                            + inventory.DeployedQuantity
                                            + inventory.NonFunctionalQuantity;
                }


                await context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Request approved and hardware marked as deployed!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            }

            return RedirectToAction("DeploymentApproval");
        }
        [HttpGet]
        public IActionResult DeploymentApproval()
        {
            var pendingRequests = context.Deployments
                .Include(d => d.Hardware)
                .Where(d => d.DeployStatus == "Pending")
                .ToList();

            return View(pendingRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateReleasedBy(int id, string ReleasedBy)
        {
            var borrower = context.Deployments.Find(id);
            if (borrower != null)
            {
                borrower.ReleasedBy = ReleasedBy;
                context.SaveChanges();
            }
            return RedirectToAction("DeploymentRequestList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateReceivedBy(int id, string ReceivedBy)
        {
            var borrower = context.Deployments.Find(id);
            if (borrower != null)
            {
                borrower.ReceivedBy = ReceivedBy;
                context.SaveChanges();
            }
            return RedirectToAction("DeploymentRequestList");
        }

        private async Task PopulateHardwareDropdowns()
        {
            var functionalHardwares = await context.Hardwares
                .Where(h => h.HardStatus == "Functional")
                .OrderBy(h => h.HardStickerNum)
                .ToListAsync();

            ViewBag.HardwareTypes = new SelectList(
                functionalHardwares,
                "HardId",
                "HardType",
                null
            );
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelRequest(int id)
        {
            var request = context.Deployments.Find(id);
            if (request == null)
            {
                return NotFound();
            }

            context.Deployments.Remove(request);
            context.SaveChanges();

            return RedirectToAction("DeploymentRequestList");
        }
        
    }
}
