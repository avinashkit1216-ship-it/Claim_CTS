using Microsoft.AspNetCore.Mvc;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Services;
using ClaimSubmission.API.Data;
using ClaimSubmission.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace ClaimSubmission.API.Controllers
{
    /// <summary>
    /// Authentication controller for user login
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtTokenService _tokenService;
        private readonly IPasswordHashService _passwordHashService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthRepository authRepository,
            IJwtTokenService tokenService,
            IPasswordHashService passwordHashService,
            ILogger<AuthController> logger)
        {
            _authRepository = authRepository;
            _tokenService = tokenService;
            _passwordHashService = passwordHashService;
            _logger = logger;
        }

        /// <summary>
        /// Login endpoint - authenticates user and returns token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>User information and authentication token</returns>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Login attempt started");

                if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    return BadRequest(new { error = "Username and password are required" });
                }

                _logger.LogDebug($"Validating credentials for user: {request.Username}");

                // Retrieve user and validate password
                User? user = null;
                try
                {
                    user = await _authRepository.ValidateCredentialsAsync(request.Username, request.Password);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during credential validation for user '{request.Username}': {ex.GetType().Name} - {ex.Message}");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "An error occurred during authentication", details = "Please try again later." });
                }
                
                if (user == null)
                {
                    _logger.LogWarning($"Failed login attempt for user '{request.Username}' - invalid credentials");
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                _logger.LogDebug($"User '{request.Username}' credentials valid. UserId: {user.UserId}");

                // Update last login
                try
                {
                    await _authRepository.UpdateLastLoginAsync(user.UserId);
                    _logger.LogDebug($"Updated last login for user: {user.UserId}");
                }
                catch (Exception loginEx)
                {
                    _logger.LogWarning(loginEx, $"Failed to update last login for user {user.UserId}");
                    // Continue despite last login update failure
                }

                // Generate JWT token
                string token;
                try
                {
                    token = _tokenService.GenerateToken(user);
                    _logger.LogDebug($"JWT token generated successfully for user: {user.UserId}");
                }
                catch (Exception tokenEx)
                {
                    _logger.LogError(tokenEx, $"Error generating JWT token for user '{request.Username}': {tokenEx.Message}");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "Token generation failed" });
                }

                var response = new
                {
                    data = new LoginResponse
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        FullName = user.FullName,
                        Email = user.Email,
                        Token = token
                    }
                };

                _logger.LogInformation($"User '{request.Username}' (ID: {user.UserId}) logged in successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during login: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, $"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Register endpoint - creates a new user account
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>User information and authentication token on success</returns>
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Registration attempt started");

                if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Registration attempt with missing required fields");
                    return BadRequest(new { error = "Email, password, and full name are required" });
                }

                if (request.Password != request.ConfirmPassword)
                {
                    _logger.LogWarning("Registration attempt with mismatched passwords");
                    return BadRequest(new { error = "Passwords do not match" });
                }

                if (request.Password.Length < 8)
                {
                    _logger.LogWarning("Registration attempt with weak password");
                    return BadRequest(new { error = "Password must be at least 8 characters long" });
                }

                if (!request.AcceptTermsAndConditions || !request.AcceptPrivacyPolicy)
                {
                    _logger.LogWarning("Registration attempt without accepting required policies");
                    return BadRequest(new { error = "You must accept the Terms & Conditions and Privacy Policy" });
                }

                // Check if email already exists
                try
                {
                    var emailExists = await _authRepository.EmailExistsAsync(request.Email);
                    if (emailExists)
                    {
                        _logger.LogWarning($"Registration attempt with existing email: {request.Email}");
                        return Conflict(new { error = "An account with this email already exists" });
                    }
                }
                catch (Exception emailCheckEx)
                {
                    _logger.LogError(emailCheckEx, "Error checking email uniqueness");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "An error occurred during registration validation" });
                }

                // Generate username if not provided
                string username = request.Username;
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = request.Email.Split('@')[0];
                }

                // Check if username already exists
                try
                {
                    var usernameExists = await _authRepository.UsernameExistsAsync(username);
                    if (usernameExists)
                    {
                        _logger.LogWarning($"Registration attempt with existing username: {username}");
                        return Conflict(new { error = "This username is already taken. Please choose another one." });
                    }
                }
                catch (Exception usernameCheckEx)
                {
                    _logger.LogError(usernameCheckEx, "Error checking username uniqueness");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "An error occurred during registration validation" });
                }

                // Hash password
                string passwordHash = "";
                try
                {
                    passwordHash = _passwordHashService.HashPassword(request.Password);
                    _logger.LogDebug($"Password hashed successfully for registration");
                }
                catch (Exception hashEx)
                {
                    _logger.LogError(hashEx, "Error hashing password during registration");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "An error occurred during password processing" });
                }

                // Create user
                int userId = 0;
                try
                {
                    userId = await _authRepository.CreateUserAsync(
                        username, 
                        passwordHash, 
                        request.Email, 
                        request.FullName ?? "");
                    
                    _logger.LogInformation($"User '{username}' registered successfully with ID: {userId}");
                }
                catch (InvalidOperationException dupEx)
                {
                    _logger.LogWarning($"Duplicate user during registration: {dupEx.Message}");
                    return Conflict(new { error = dupEx.Message });
                }
                catch (Exception regEx)
                {
                    _logger.LogError(regEx, $"Error creating user during registration: {regEx.Message}");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "An error occurred while creating your account. Please try again." });
                }

                // Retrieve created user
                User? newUser = null;
                try
                {
                    newUser = await _authRepository.ValidateCredentialsAsync(username, request.Password);
                }
                catch (Exception retrieveEx)
                {
                    _logger.LogError(retrieveEx, "Error retrieving user after registration");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "Account created but unable to complete login" });
                }

                if (newUser == null)
                {
                    _logger.LogError("User not found after creation");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "Account created but unable to complete login" });
                }

                // Generate JWT token
                string token;
                try
                {
                    token = _tokenService.GenerateToken(newUser);
                    _logger.LogDebug($"JWT token generated successfully for registered user: {username}");
                }
                catch (Exception tokenEx)
                {
                    _logger.LogError(tokenEx, $"Error generating JWT token during registration: {tokenEx.Message}");
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "Account created but unable to generate authentication token" });
                }

                var response = new
                {
                    data = new LoginResponse
                    {
                        UserId = newUser.UserId,
                        Username = newUser.Username,
                        FullName = newUser.FullName,
                        Email = newUser.Email,
                        Token = token
                    }
                };

                _logger.LogInformation($"User '{username}' (ID: {newUser.UserId}) registered and logged in successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during registration: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, $"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Logout endpoint - invalidates user session
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Logout()
        {
            try
            {
                _logger.LogInformation("User logout request received");
                
                // Return successful logout response
                return Ok(new { message = "Logout successful", success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during logout: {ex.GetType().Name} - {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "An error occurred during logout" });
            }
        }
    }
}

