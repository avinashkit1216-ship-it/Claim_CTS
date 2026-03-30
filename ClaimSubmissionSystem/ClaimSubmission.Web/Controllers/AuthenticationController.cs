using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;

namespace ClaimSubmission.Web.Controllers
{
    /// <summary>
    /// Authentication controller for user login/logout
    /// </summary>
    public class AuthenticationController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(IAuthenticationService authService, ILogger<AuthenticationController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Display login page
        /// </summary>
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") == "true")
            {
                return RedirectToAction("Index", "Claim");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>
        /// Handle login submission
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                UserViewModel? user = await _authService.LoginAsync(model);

                if (user != null)
                {
                    // Store user information in session
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
                    HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty);
                    HttpContext.Session.SetString("Email", user.Email ?? string.Empty);
                    HttpContext.Session.SetString("UserToken", user.Token ?? string.Empty);
                    HttpContext.Session.SetString("IsAuthenticated", "true");

                    _logger.LogInformation($"User '{model.Username}' logged in successfully");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Claim");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password");
                    _logger.LogWarning($"Failed login attempt for user '{model.Username}'");
                }
            }
            catch (UnauthorizedAccessException)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                _logger.LogWarning($"Unauthorized access for user '{model.Username}'");
            }
            catch (HttpRequestException hEx) when (hEx.InnerException is TimeoutException || 
                                                     hEx.Message.Contains("unavailable") ||
                                                     hEx.Message.Contains("connection"))
            {
                ModelState.AddModelError(string.Empty, "Authentication service is currently unavailable. Please try again later.");
                _logger.LogWarning(hEx, "API service unavailable during login attempt");
            }
            catch (HttpRequestException hEx)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect to authentication service. Please check your network connection.");
                _logger.LogError(hEx, "HttpRequest error during login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login: {ex.GetType().Name} - {ex.Message}");
                ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            }

            return View(model);
        }

        /// <summary>
        /// Display register page
        /// </summary>
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") == "true")
            {
                return RedirectToAction("Index", "Claim");
            }

            return View(new RegisterViewModel());
        }

        /// <summary>
        /// Handle registration submission
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Check if this is a JSON request (AJAX) or form submission
            bool isJsonRequest = HttpContext.Request.ContentType?.Contains("application/json") ?? false;

            if (!ModelState.IsValid)
            {
                if (isJsonRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }
                return View(model);
            }

            try
            {
                // If username is not provided, use email as username
                if (string.IsNullOrWhiteSpace(model.Username))
                {
                    model.Username = model.Email?.Split('@')[0] ?? model.Email;
                }

                // Call registration API
                UserViewModel? user = await _authService.RegisterAsync(model);

                if (user != null)
                {
                    _logger.LogInformation($"User '{model.Email}' registered successfully");
                    
                    // Store user information in session
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
                    HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty);
                    HttpContext.Session.SetString("Email", user.Email ?? string.Empty);
                    HttpContext.Session.SetString("UserToken", user.Token ?? string.Empty);
                    HttpContext.Session.SetString("IsAuthenticated", "true");

                    if (isJsonRequest)
                    {
                        // Return JSON response for AJAX requests
                        return Json(new
                        {
                            success = true,
                            message = "Registration successful!",
                            data = user
                        });
                    }
                    else
                    {
                        // Redirect to login for form submissions
                        TempData["SuccessMessage"] = "Registration successful! Please log in with your credentials.";
                        return RedirectToAction("Login");
                    }
                }
                else
                {
                    var errorMsg = "Registration failed. Please try again.";
                    _logger.LogWarning($"Failed registration attempt for email '{model.Email}'");
                    
                    if (isJsonRequest)
                    {
                        return Json(new
                        {
                            success = false,
                            message = errorMsg
                        });
                    }
                    
                    ModelState.AddModelError(string.Empty, errorMsg);
                }
            }
            catch (InvalidOperationException ioEx)
            {
                var errorMsg = ioEx.Message;
                _logger.LogWarning(ioEx, $"Registration validation error for '{model.Email}'");
                
                if (isJsonRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = errorMsg
                    });
                }
                
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            catch (UnauthorizedAccessException)
            {
                var errorMsg = "Registration is currently unavailable. Please try again later.";
                _logger.LogWarning($"Unauthorized access during registration for '{model.Email}'");
                
                if (isJsonRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = errorMsg
                    });
                }
                
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            catch (HttpRequestException hEx) when (hEx.InnerException is TimeoutException || 
                                                     hEx.Message.Contains("unavailable") ||
                                                     hEx.Message.Contains("connection"))
            {
                var errorMsg = "Registration service is currently unavailable. Please try again later.";
                _logger.LogWarning(hEx, "API service unavailable during registration");
                
                if (isJsonRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = errorMsg
                    });
                }
                
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            catch (HttpRequestException hEx)
            {
                var errorMsg = "Unable to connect to registration service. Please check your network connection.";
                _logger.LogError(hEx, "HttpRequest error during registration");
                
                if (isJsonRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = errorMsg
                    });
                }
                
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            catch (Exception ex)
            {
                var errorMsg = "An error occurred during registration. Please try again.";
                _logger.LogError(ex, $"Error during registration: {ex.GetType().Name} - {ex.Message}");
                
                if (isJsonRequest)
                {
                    return Json(new
                    {
                        success = false,
                        message = errorMsg
                    });
                }
                
                ModelState.AddModelError(string.Empty, errorMsg);
            }

            if (isJsonRequest)
            {
                return Json(new
                {
                    success = false,
                    message = "Registration failed. Please try again."
                });
            }

            return View(model);
        }

        /// <summary>
        /// Logout user
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                
                // Clear session
                HttpContext.Session.Clear();
                
                _logger.LogInformation($"User '{username}' logged out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }

            return RedirectToAction("Login");
        }
    }
}