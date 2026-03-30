using Microsoft.AspNetCore.Mvc;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Services;
using ClaimSubmission.API.Data;
using ClaimSubmission.API.Models;

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
    }
}

