using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;

namespace ClaimSubmission.Web.Controllers
{
    
    public class AuthenticationController : Controller
    {
        private readonly IAuthenticationService _authService;

        public AuthenticationController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        public IActionResult Login() {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                UserViewModel user = await _authService.LoginAsync(model);

                if (user != null)
                {
                    // Store user information in session
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("UserName", user.UserName);
                    HttpContext.Session.SetString("FullName", user.FullName);
                    HttpContext.Session.SetString("UserToken", user.Token);
                    HttpContext.Session.SetString("IsAuthenticated", "true");

                    // Note: For persistent authentication, consider using cookie authentication middleware
                    // with claims-based identity instead of sessions

                    return RedirectToAction("List", "Claim");
                }
                else
                {
                    ViewBag.Error = "Invalid username or password";
                    return View(model);
                }
            }
            catch (UnauthorizedAccessException)
            {
                ViewBag.Error = "Invalid username or password";
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Login error: {ex.Message}";
                return View(model);
            }
        }

        /// <summary>
        /// GET: Authentication/Logout
        /// Clear user session and log out
        /// </summary>
        [Authorize]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}