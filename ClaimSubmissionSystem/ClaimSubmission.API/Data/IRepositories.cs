using ClaimSubmission.API.DTOs;
using ClaimSubmission.API.Models;

namespace ClaimSubmission.API.Data
{
    /// <summary>
    /// Interface for Claims repository
    /// </summary>
    public interface IClaimsRepository
    {
        Task<Claim?> GetClaimByIdAsync(int claimId);
        Task<Claim?> GetClaimByNumberAsync(string claimNumber);
        Task<PaginatedClaimsResponse> GetClaimsAsync(GetClaimsRequest request);
        Task<int> GetClaimsCountAsync(string? searchTerm = null, string? claimStatus = null);
        Task<int> CreateClaimAsync(CreateClaimRequest request, int userId);
        Task UpdateClaimAsync(int claimId, UpdateClaimRequest request, int userId);
        Task DeleteClaimAsync(int claimId);
    }

    /// <summary>
    /// Interface for Authentication repository
    /// </summary>
    public interface IAuthRepository
    {
        Task<User?> ValidateCredentialsAsync(string username, string passwordHash);
        Task<int> CreateUserAsync(string username, string passwordHash, string email, string fullName);
        Task UpdateLastLoginAsync(int userId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
    }
}
