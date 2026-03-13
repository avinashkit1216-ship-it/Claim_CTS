using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;
using ClaimSubmission.MVC.Services;

namespace ClaimSubmission.Web.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly IClaimApiService _claimApiService;
        private readonly IConfiguration _configuration;

        public ClaimController(IConfiguration configuration)
        {
            _configuration = configuration;
            string apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:9090";
            _claimApiService = new ClaimApiService(apiBaseUrl);
        }

        // Display List of Claims
        public async Task<IActionResult> List(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                string token = GetUserToken();
                List<ClaimViewListModel> claims = await _claimApiService.GetAllClaimsAsync(token, pageNumber, pageSize);
                return View(claims);
            }
            catch (Exception e)
            {
                ViewBag.Error = $"Error loading claims: {e.Message}";
                return View(new List<ClaimViewListModel>());
            }
        }

        // GET: Claim/Add
        public IActionResult Add()
        {
            return View(new AddClaimViewModel());
        }

        // POST: Claim/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var err in modelState.Errors)
                    {
                        errors.Add(err.ErrorMessage);
                    }
                }
                ViewBag.ValidationErrors = errors;
                return View(model);
            }

            try
            {
                string token = GetUserToken();
                int claimId = await _claimApiService.CreateClaimAsync(token, model);

                if (claimId > 0)
                {
                    TempData["Message"] = "Claim added successfully";
                    return RedirectToAction("List");
                }
                else
                {
                    ViewBag.Error = "Failed to create claim";
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error creating claim: {ex.Message}";
                return View(model);
            }
        }

        // GET: Claim/Edit
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                string token = GetUserToken();
                ClaimViewListModel claim = await _claimApiService.GetClaimByIdAsync(token, id);
                if (claim == null)
                {
                    return NotFound();
                }

                var editmodel = new EditClaimViewModel
                {
                    ClaimId = claim.ClaimId,
                    ClaimAmount = claim.ClaimAmount,
                    ClaimNumber = claim.ClaimNumber,
                    ClaimStatus = claim.ClaimStatus,
                    DateOfService = claim.DateOfService,
                    PatientName = claim.PatientName,
                    ProviderName = claim.ProviderName,
                };
                return View(editmodel);
            }
            catch (Exception e)
            {
                ViewBag.Error = $"Error loading claim: {e.Message}";
                return RedirectToAction("List");
            }
        }

        // POST: Claim/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditClaimViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                ViewBag.ValidationErrors = errors;
                return View(model);
            }

            try
            {
                model.ClaimId = id;
                string token = GetUserToken();
                await _claimApiService.UpdateClaimAsync(token, model);

                TempData["Message"] = "Claim updated successfully";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error updating claim: {ex.Message}";
                return View(model);
            }
        }

        // POST: Claim/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                string token = GetUserToken();
                await _claimApiService.DeleteClaimAsync(token, id);

                TempData["Message"] = "Claim deleted successfully";
                return RedirectToAction("List");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error deleting claim: {ex.Message}";
                return RedirectToAction("List");
            }
        }

        /// <summary>
        /// Helper method to get user token from session
        /// </summary>
        private string GetUserToken()
        {
            return HttpContext.Session.GetString("UserToken") ?? string.Empty;
        }
    }
}
