using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Threading.Tasks;
// using Newtonsoft.Json;
using ClaimSubmission.Web.Models;

namespace ClaimSubmission.MVC.Services
{
    /// <summary>
    /// Service to communicate with Web API for Claim operations
    /// </summary>
    public interface IClaimApiService
    {
        Task<List<ClaimListViewModel>> GetAllClaimsAsync(string token, int pageNumber = 1, int pageSize = 10);
        Task<ClaimListViewModel> GetClaimByIdAsync(string token, int claimId);
        Task<int> CreateClaimAsync(string token, AddClaimViewModel claim);
        Task UpdateClaimAsync(string token, EditClaimViewModel claim);
        Task DeleteClaimAsync(string token, int claimId);
    }

    public class ClaimApiService : IClaimApiService
    {
        private readonly string _apiBaseUrl;
        private readonly HttpClient _httpClient;

        public ClaimApiService(string apiBaseUrl)
        {
            _apiBaseUrl = apiBaseUrl;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Get all claims from API
        /// </summary>
        public async Task<List<ClaimListViewModel>> GetAllClaimsAsync(string token, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims?pageNumber={pageNumber}&pageSize={pageSize}";

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
                            var content = await response.Content.ReadAsStringAsync();
                            dynamic result = JsonConvert.DeserializeObject(content);
                            
                            if (result != null && result.data != null && result.data.items != null)
                            {
                                return JsonConvert.DeserializeObject<List<ClaimListViewModel>>(
                                    result.data.items.ToString());
                            }
                        }
                        else
                        {
                            throw new Exception($"API Error: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving claims: {ex.Message}", ex);
            }

            return new List<ClaimListViewModel>();
        }

        /// <summary>
        /// Get specific claim by ID from API
        /// </summary>
        public async Task<ClaimListViewModel> GetClaimByIdAsync(string token, int claimId)
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
                            var content = await response.Content.ReadAsStringAsync();
                            dynamic result = JsonConvert.DeserializeObject(content);

                            if (result != null && result.data != null)
                            {
                                return JsonConvert.DeserializeObject<ClaimListViewModel>(
                                    result.data.ToString());
                            }
                        }
                        else
                        {
                            throw new Exception($"Claim not found");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving claim: {ex.Message}", ex);
            }

            return null;
        }

        /// <summary>
        /// Create a new claim via API
        /// </summary>
        public async Task<int> CreateClaimAsync(string token, AddClaimViewModel claim)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims";

                var content = new StringContent(
                    JsonConvert.SerializeObject(claim),
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = content;
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            dynamic result = JsonConvert.DeserializeObject(responseContent);

                            if (result != null && result.data != null)
                            {
                                return Convert.ToInt32(result.data.claimId);
                            }
                        }
                        else
                        {
                            throw new Exception($"API Error: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating claim: {ex.Message}", ex);
            }

            return -1;
        }

        /// <summary>
        /// Update an existing claim via API
        /// </summary>
        public async Task UpdateClaimAsync(string token, EditClaimViewModel claim)
        {
            try
            {
                string url = $"{_apiBaseUrl}/api/claims/{claim.ClaimId}";

                var content = new StringContent(
                    JsonConvert.SerializeObject(claim),
                    System.Text.Encoding.UTF8,
                    "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    request.Content = content;
                    if (!string.IsNullOrEmpty(token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"API Error: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating claim: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Delete a claim via API
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
                            throw new Exception($"API Error: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting claim: {ex.Message}", ex);
            }
        }
    }
}
