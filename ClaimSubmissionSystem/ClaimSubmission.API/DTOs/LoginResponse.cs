namespace ClaimSubmission.API.DTOs
{
    /// <summary>
    /// Login response model
    /// </summary>
    public class LoginResponse
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Token { get; set; }
    }
}
