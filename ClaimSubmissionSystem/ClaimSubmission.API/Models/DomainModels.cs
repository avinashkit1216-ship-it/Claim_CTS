namespace ClaimSubmission.API.Models
{
    /// <summary>
    /// Domain model for Claims
    /// </summary>
    public class Claim
    {
        public int ClaimId { get; set; }
        public string? ClaimNumber { get; set; }
        public string? PatientName { get; set; }
        public string? ProviderName { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal ClaimAmount { get; set; }
        public string? ClaimStatus { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? LastModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
    }

    /// <summary>
    /// Domain model for Users
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
