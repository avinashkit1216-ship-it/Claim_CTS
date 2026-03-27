using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClaimSubmission.Web.Models;

namespace ClaimSubmission.Web.Services
{
    /// <summary>
    /// Interface for Claims API service
    /// </summary>
    public interface IClaimApiService
    {
        Task<ClaimsPaginatedListViewModel?> GetClaimsAsync(string token, int pageNumber = 1, int pageSize = 20, 
            string? searchTerm = null, string? claimStatus = null, string? sortBy = "CreatedDate", string? sortDirection = "DESC");
        Task<EditClaimViewModel?> GetClaimByIdAsync(string token, int claimId);
        Task<int> CreateClaimAsync(string token, CreateClaimViewModel claim);
        Task UpdateClaimAsync(string token, EditClaimViewModel claim);
        Task DeleteClaimAsync(string token, int claimId);
    }

    /// <summary>
    /// Claim API service for API communication
    /// </summary>
    public class ClaimApiService : IClaimApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;
        private readonly ILogger<ClaimApiService>? _logger;

        public ClaimApiService(HttpClient httpClient, string apiBaseUrl, ILogger<ClaimApiService>? logger = null)
        {
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl;
            _logger = logger;
        }

        /// <summary>
        /// Get paginated claims from API
        /// </summary>
        public async Task<ClaimsPaginatedListViewModel?> GetClaimsAsync(string token, int pageNumber = 1, int pageSize = 20,
            string? searchTerm = null, string? claimStatus = null, string? sortBy = "CreatedDate", string? sortDirection = "DESC")
        {
            try
            {
                // Ensure PageSize is always positive to prevent divide-by-zero
                if (pageSize <= 0) pageSize = 20;
                if (pageNumber <= 0) pageNumber = 1;

                var queryParams = new Dictionary<string, string>
                {
                    { "pageNumber", pageNumber.ToString() },
                    { "pageSize", pageSize.ToString() },
                    { "sortBy", sortBy ?? "CreatedDate" },
                    { "sortDirection", sortDirection ?? "DESC" }
                };

                if (!string.IsNullOrEmpty(searchTerm))
                    queryParams["searchTerm"] = searchTerm;
                if (!string.IsNullOrEmpty(claimStatus))
                    queryParams["claimStatus"] = claimStatus;

                string url = $"{_apiBaseUrl}/api/claims?{BuildQueryString(queryParams)}";

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrWhiteSpace(responseContent))
                            {
                                _logger?.LogWarning("Empty response from API");
                                return new ClaimsPaginatedListViewModel
                                {
                                    Claims = new List<ClaimViewListModel>(),
                                    TotalRecords = 0,
                                    PageNumber = pageNumber,
                                    PageSize = pageSize
                                };
                            }

                            using (JsonDocument doc = JsonDocument.Parse(responseContent))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    var paginatedData = JsonSerializer.Deserialize<ApiPaginatedResponse<ClaimViewListModel>>(
                                        dataElement.GetRawText(),
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                    if (paginatedData != null)
                                    {
                                        return new ClaimsPaginatedListViewModel
                                        {
                                            Claims = paginatedData.Claims ?? new List<ClaimViewListModel>(),
                                            TotalRecords = paginatedData.TotalRecords,
                                            PageNumber = pageNumber,
                                            PageSize = pageSize,
                                            SearchTerm = searchTerm,
                                            ClaimStatus = claimStatus,
                                            SortBy = sortBy,
                                            SortDirection = sortDirection
                                        };
                                    }
                                }
                            }
                        }
                        else
                        {
                            string errorMessage = $"API returned error: {response.StatusCode}";
                            try
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                _logger?.LogError($"{errorMessage}. Details: {errorContent}");
                            }
                            catch
                            {
                                _logger?.LogError(errorMessage);
                            }

                            // Return empty list instead of throwing to prevent app crash
                            return new ClaimsPaginatedListViewModel
                            {
                                Claims = new List<ClaimViewListModel>(),
                                TotalRecords = 0,
                                PageNumber = pageNumber,
                                PageSize = pageSize
                            };
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request error retrieving claims");
                return new ClaimsPaginatedListViewModel
                {
                    Claims = new List<ClaimViewListModel>(),
                    TotalRecords = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "JSON parsing error retrieving claims");
                return new ClaimsPaginatedListViewModel
                {
                    Claims = new List<ClaimViewListModel>(),
                    TotalRecords = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error retrieving claims");
                return new ClaimsPaginatedListViewModel
                {
                    Claims = new List<ClaimViewListModel>(),
                    TotalRecords = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            // Default return if no other condition is met
            return new ClaimsPaginatedListViewModel
            {
                Claims = new List<ClaimViewListModel>(),
                TotalRecords = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Get specific claim by ID from API
        /// </summary>
        public async Task<EditClaimViewModel?> GetClaimByIdAsync(string token, int claimId)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims/{claimId}";

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrWhiteSpace(responseContent))
                                return null;

                            using (JsonDocument doc = JsonDocument.Parse(responseContent))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    return JsonSerializer.Deserialize<EditClaimViewModel>(
                                        dataElement.GetRawText(),
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                }
                            }
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            return null;
                        }
                        else
                        {
                            _logger?.LogError($"API Error: {response.StatusCode}");
                            throw new Exception($"API Error: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error retrieving claim");
                throw;
            }

            return null;
        }

        /// <summary>
        /// Create a new claim via API
        /// </summary>
        public async Task<int> CreateClaimAsync(string token, CreateClaimViewModel claim)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims";

                var jsonContent = JsonSerializer.Serialize(claim);
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            if (string.IsNullOrWhiteSpace(responseContent))
                                return 0;

                            using (JsonDocument doc = JsonDocument.Parse(responseContent))
                            {
                                JsonElement root = doc.RootElement;
                                if (root.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    if (dataElement.TryGetProperty("claimId", out JsonElement claimIdElement))
                                    {
                                        return claimIdElement.GetInt32();
                                    }
                                }
                            }
                        }
                        else
                        {
                            _logger?.LogError($"Create claim API Error: {response.StatusCode}");
                            throw new Exception($"Failed to create claim: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating claim");
                throw;
            }

            return 0;
        }

        /// <summary>
        /// Update claim via API
        /// </summary>
        public async Task UpdateClaimAsync(string token, EditClaimViewModel claim)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims/{claim.ClaimId}";

                var jsonContent = JsonSerializer.Serialize(claim);
                var content = new StringContent(
                    jsonContent,
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content })
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogError($"Update claim API Error: {response.StatusCode}");
                            throw new Exception($"Failed to update claim: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating claim");
                throw;
            }
        }

        /// <summary>
        /// Delete claim via API
        /// </summary>
        public async Task DeleteClaimAsync(string token, int claimId)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims/{claimId}";

                using (var request = new HttpRequestMessage(HttpMethod.Delete, url))
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            _logger?.LogError($"Delete claim API Error: {response.StatusCode}");
                            throw new Exception($"Failed to delete claim: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting claim");
                throw;
            }
        }

        /// <summary>
        /// Build query string from parameters
        /// </summary>
        private string BuildQueryString(Dictionary<string, string> parameters)
        {
            var query = new System.Text.StringBuilder();
            foreach (var param in parameters)
            {
                if (query.Length > 0)
                    query.Append("&");
                query.Append($"{System.Net.WebUtility.UrlEncode(param.Key)}={System.Net.WebUtility.UrlEncode(param.Value)}");
            }
            return query.ToString();
        }
    }
}
