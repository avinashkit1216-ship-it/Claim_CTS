using Microsoft.AspNetCore.Mvc;
using ClaimSubmission.API.DTOs;

namespace ClaimSubmission.API.Controllers
{
    /// <summary>
    /// Authentication controller for user login
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger)
        {
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
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    return BadRequest(new { error = "Username and password are required" });
                }

                // TODO: Implement actual user authentication with database
                // This is a placeholder implementation for demonstration
                if (request.Username == "admin" && request.Password == "admin123")
                {
                    var response = new
                    {
                        data = new LoginResponse
                        {
                            UserId = 1,
                            Username = request.Username,
                            FullName = "Administrator",
                            Token = GenerateToken(request.Username)
                        }
                    };

                    _logger.LogInformation($"User '{request.Username}' logged in successfully");
                    return Ok(response);
                }

                _logger.LogWarning($"Failed login attempt for user '{request.Username}'");
                return Unauthorized(new { error = "Invalid username or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Generate a simple JWT-like token (placeholder)
        /// </summary>
        private string GenerateToken(string username)
        {
            // TODO: Implement proper JWT token generation
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{DateTime.UtcNow.Ticks}"));
        }
    }
}
