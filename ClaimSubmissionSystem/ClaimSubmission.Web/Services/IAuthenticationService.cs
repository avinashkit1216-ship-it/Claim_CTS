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
                    throw new ArgumentException("Username and password are required");
                }

                string url = $"{_apiBaseUrl}/api/auth/login";

                var payload = new { username = login.Username, password = login.Password };
                var jsonContent = JsonSerializer.Serialize(payload);
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var response = await _httpClient.PostAsync(url, content))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            throw new Exception("Empty response from API");
                        }

                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(responseContent))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    return new UserViewModel
                                    {
                                        UserId = dataElement.GetProperty("userId").GetInt32(),
                                        Username = dataElement.GetProperty("username").GetString(),
                                        FullName = dataElement.GetProperty("fullName").GetString(),
                                        Email = dataElement.GetProperty("email").GetString(),
                                        Token = dataElement.GetProperty("token").GetString()
                                    };
                                }
                                else
                                {
                                    throw new Exception("Invalid API response structure");
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            throw new Exception($"Failed to parse API response: {ex.Message}", ex);
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Invalid username or password");
                    }
                    else
                    {
                        throw new Exception($"Login failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                throw new Exception($"Error during login: {ex.Message}", ex);
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

