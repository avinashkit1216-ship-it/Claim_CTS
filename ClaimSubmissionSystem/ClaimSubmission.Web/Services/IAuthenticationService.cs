using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ClaimSubmission.Web.Models;

namespace ClaimSubmission.Web.Services
{
    /// <summary>
    /// Interface for authentication service
    /// </summary>
    public interface IAuthenticationService
    {
        Task<UserViewModel?> LoginAsync(LoginViewModel login);
        Task<UserViewModel?> RegisterAsync(RegisterViewModel register);
    }

    /// <summary>
    /// Authentication service for API communication
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(HttpClient httpClient, string apiBaseUrl, ILogger<AuthenticationService>? logger = null)
        {
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl;
            _logger = logger ?? new NullLogger<AuthenticationService>();
        }

        public async Task<UserViewModel?> LoginAsync(LoginViewModel login)
        {
            try
            {
                if (login == null || string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrWhiteSpace(login.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    throw new ArgumentException("Username and password are required");
                }

                string url = $"{_apiBaseUrl}/api/auth/login";
                _logger.LogDebug($"Initiating login request to: {url}");

                var payload = new { username = login.Username, password = login.Password };
                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var response = await _httpClient.PostAsync(url, content))
                {
                    _logger.LogDebug($"Login response status code: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            _logger.LogError("Empty response from API after successful status code");
                            throw new Exception("Empty response from API");
                        }

                        try
                        {
                            _logger.LogDebug("Parsing login response JSON");
                            using (JsonDocument doc = JsonDocument.Parse(responseContent))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    var user = new UserViewModel
                                    {
                                        UserId = dataElement.GetProperty("userId").GetInt32(),
                                        Username = dataElement.GetProperty("username").GetString(),
                                        FullName = dataElement.GetProperty("fullName").GetString(),
                                        Email = dataElement.GetProperty("email").GetString(),
                                        Token = dataElement.GetProperty("token").GetString()
                                    };
                                    
                                    _logger.LogInformation($"Successfully parsed login response for user: {user.Username}");
                                    return user;
                                }
                                else
                                {
                                    _logger.LogError("Invalid API response structure - 'data' property not found");
                                    throw new Exception("Invalid API response structure");
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to parse API response as JSON");
                            throw new Exception($"Failed to parse API response: {ex.Message}", ex);
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning($"Login failed with 401 Unauthorized for user: {login.Username}");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"API response: {responseContent}");
                        throw new UnauthorizedAccessException("Invalid username or password");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        _logger.LogWarning($"Login failed with 400 BadRequest");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"API response: {responseContent}");
                        throw new ArgumentException($"Bad request: {responseContent}");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        _logger.LogError($"API returned 500 InternalServerError");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Server error response: {responseContent}");
                        throw new Exception($"Server error: {responseContent}");
                    }
                    else
                    {
                        _logger.LogError($"Unexpected response status code: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Response content: {responseContent}");
                        throw new Exception($"Login failed with status {response.StatusCode}: {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        public async Task<UserViewModel?> RegisterAsync(RegisterViewModel register)
        {
            try
            {
                if (register == null || string.IsNullOrWhiteSpace(register.Email) || string.IsNullOrWhiteSpace(register.Password))
                {
                    _logger.LogWarning("Registration attempt with missing required fields");
                    throw new ArgumentException("Email and password are required");
                }

                if (register.Password != register.ConfirmPassword)
                {
                    _logger.LogWarning("Registration attempt with mismatched passwords");
                    throw new ArgumentException("Passwords do not match");
                }

                string url = $"{_apiBaseUrl}/api/auth/register";
                _logger.LogDebug($"Initiating registration request to: {url}");

                var payload = new
                {
                    fullName = register.FullName,
                    email = register.Email,
                    phoneNumber = register.PhoneNumber,
                    username = register.Username,
                    password = register.Password,
                    dateOfBirth = register.DateOfBirth,
                    gender = register.Gender,
                    country = register.Country,
                    referralCode = register.ReferralCode,
                    acceptTermsAndConditions = register.AcceptTermsAndConditions,
                    acceptPrivacyPolicy = register.AcceptPrivacyPolicy,
                    acceptMarketingEmails = register.AcceptMarketingEmails
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var response = await _httpClient.PostAsync(url, content))
                {
                    _logger.LogDebug($"Registration response status code: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            _logger.LogError("Empty response from API after successful registration");
                            throw new Exception("Empty response from API");
                        }

                        try
                        {
                            _logger.LogDebug("Parsing registration response JSON");
                            using (JsonDocument doc = JsonDocument.Parse(responseContent))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    var user = new UserViewModel
                                    {
                                        UserId = dataElement.GetProperty("userId").GetInt32(),
                                        Username = dataElement.GetProperty("username").GetString(),
                                        FullName = dataElement.GetProperty("fullName").GetString(),
                                        Email = dataElement.GetProperty("email").GetString(),
                                        Token = dataElement.GetProperty("token").GetString()
                                    };

                                    _logger.LogInformation($"Successfully registered user: {user.Email}");
                                    return user;
                                }
                                else
                                {
                                    _logger.LogError("Invalid API response structure - 'data' property not found");
                                    throw new Exception("Invalid API response structure");
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError(ex, "Failed to parse API response as JSON");
                            throw new Exception($"Failed to parse API response: {ex.Message}", ex);
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        _logger.LogWarning($"Registration failed with 400 BadRequest");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"API response: {responseContent}");
                        throw new InvalidOperationException($"Registration failed: {responseContent}");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        _logger.LogWarning($"Registration failed with 409 Conflict - Email already exists");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"API response: {responseContent}");
                        throw new InvalidOperationException("An account with this email already exists");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning($"Registration failed with 401 Unauthorized");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogDebug($"API response: {responseContent}");
                        throw new UnauthorizedAccessException("Registration service is unavailable");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        _logger.LogError($"API returned 500 InternalServerError during registration");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Server error response: {responseContent}");
                        throw new Exception($"Server error: {responseContent}");
                    }
                    else
                    {
                        _logger.LogError($"Unexpected response status code: {response.StatusCode}");
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Response content: {responseContent}");
                        throw new Exception($"Registration failed with status {response.StatusCode}: {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during registration: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Null logger implementation for cases where ILogger is not available
    /// </summary>
    internal class NullLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}

