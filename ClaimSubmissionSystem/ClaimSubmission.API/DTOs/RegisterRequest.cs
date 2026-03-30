namespace ClaimSubmission.API.DTOs
{
    /// <summary>
    /// User registration request model
    /// </summary>
    public class RegisterRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Country { get; set; }
        public string? ReferralCode { get; set; }
        public bool AcceptTermsAndConditions { get; set; }
        public bool AcceptPrivacyPolicy { get; set; }
        public bool AcceptMarketingEmails { get; set; }
    }
}
