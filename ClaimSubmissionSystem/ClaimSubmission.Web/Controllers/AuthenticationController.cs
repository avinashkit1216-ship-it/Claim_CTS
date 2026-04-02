using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ClaimSubmission.Web.Models;
using ClaimSubmission.Web.Services;

namespace ClaimSubmission.Web.Controllers
{
    /// <summary>
    /// Authentication controller for user login/logout with cookie-based authentication
    /// </summary>
    public class AuthenticationController : Controller
    {
        private readonly Services.IAuthenticationService _authService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(Services.IAuthenticationService authService, ILogger<AuthenticationController> logger)
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
            // If already authenticated, redirect to claims
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Claim");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>
        /// Handle login submission using ASP.NET Core cookie authentication
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
                    // Create claims for the authenticated user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username ?? ""),
                        new Claim("FullName", user.FullName ?? ""),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim("UserToken", user.Token ?? "")
                    };

                    // Create claims identity
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false, // Session cookie only
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    // Sign in with cookie authentication
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Also store in session for backward compatibility with custom middleware
                    HttpContext.Session.SetString("UserId", user.UserId.ToString());
                    HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
                    HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty);
                    HttpContext.Session.SetString("Email", user.Email ?? string.Empty);
                    HttpContext.Session.SetString("UserToken", user.Token ?? string.Empty);
                    HttpContext.Session.SetString("IsAuthenticated", "true");

                    _logger.LogInformation($"User '{model.Username}' (ID: {user.UserId}) logged in successfully from {HttpContext.Connection.RemoteIpAddress}");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Claim");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password");
                    _logger.LogWarning($"Failed login attempt for user '{model.Username}' from {HttpContext.Connection.RemoteIpAddress}");
                }
            }
            catch (UnauthorizedAccessException)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                _logger.LogWarning($"Unauthorized access for user '{model.Username}' from {HttpContext.Connection.RemoteIpAddress}");
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
                _logger.LogError(ex, $"Unexpected error during login: {ex.GetType().Name} - {ex.Message}");
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
        /// Handle registration submission with automatic login
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
                    _logger.LogInformation($"User '{model.Email}' (ID: {user.UserId}) registered successfully from {HttpContext.Connection.RemoteIpAddress}");
                    
                    // Create claims for the newly registered user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Username ?? ""),
                        new Claim("FullName", user.FullName ?? ""),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim("UserToken", user.Token ?? "")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    // Sign in new user
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Also store in session for backward compatibility
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
                            message = "Registration and login successful!",
                            data = user
                        });
                    }
                    else
                    {
                        // Redirect to claims page for form submissions
                        return RedirectToAction("Index", "Claim");
                    }
                }
                else
                {
                    var errorMsg = "Registration failed. Please try again.";
                    _logger.LogWarning($"Failed registration attempt for email '{model.Email}' from {HttpContext.Connection.RemoteIpAddress}");
                    
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
                _logger.LogError(ex, $"Unexpected error during registration: {ex.GetType().Name} - {ex.Message}");
                
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
        /// Logout user - clears authentication cookie and session
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                
                // Sign out from cookie authentication
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                // Clear session as well for backward compatibility
                HttpContext.Session.Clear();
                
                _logger.LogInformation($"User '{username}' (ID: {userId}) logged out successfully from {HttpContext.Connection.RemoteIpAddress}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during logout: {ex.GetType().Name} - {ex.Message}");
            }

            return RedirectToAction("Login");
        }

        /// <summary>
        /// Access Denied page
        /// </summary>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}