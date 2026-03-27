using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
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
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
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
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, $"SQL Server connection error during credential validation for user '{request.Username}': {sqlEx.Message}");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                        new { error = "Database service unavailable", details = "Unable to connect to database. Please try again later." });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, $"Database error during credential validation for user '{request.Username}': {dbEx.GetType().Name} - {dbEx.Message}");
                    // Check if it's a connection-related error
                    if (IsConnectionError(dbEx))
                    {
                        return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                            new { error = "Database service unavailable", details = "Unable to reach database. Please try again later." });
                    }
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "Database error occurred", details = "An error occurred while processing your request." });
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
        /// Helper method to determine if an exception is connection-related
        /// </summary>
        private bool IsConnectionError(Exception ex)
        {
            var message = ex.Message?.ToLower() ?? "";
            var innerExceptionMessage = ex.InnerException?.Message?.ToLower() ?? "";
            var combinedMessage = message + " " + innerExceptionMessage;

            return combinedMessage.Contains("connection") || 
                   combinedMessage.Contains("timeout") || 
                   combinedMessage.Contains("server") ||
                   combinedMessage.Contains("network") ||
                   combinedMessage.Contains("unavailable") ||
                   combinedMessage.Contains("connect") ||
                   combinedMessage.Contains("tcp");
        }
    }
}

