using Microsoft.AspNetCore.Mvc;
using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Services;
using ClaimSubmission.API.Data;

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
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    return BadRequest(new { error = "Username and password are required" });
                }

                // Retrieve user and validate password
                var user = await _authRepository.ValidateCredentialsAsync(request.Username, request.Password);
                
                if (user == null)
                {
                    _logger.LogWarning($"Failed login attempt for user '{request.Username}'");
                    return Unauthorized(new { error = "Invalid username or password" });
                }

                // Update last login
                await _authRepository.UpdateLastLoginAsync(user.UserId);

                // Generate JWT token
                var token = _tokenService.GenerateToken(user);

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

                _logger.LogInformation($"User '{request.Username}' logged in successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "An error occurred during login" });
            }
        }
    }
}

