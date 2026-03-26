namespace ClaimSubmission.Web.Services
{
    /// <summary>
    /// View model for authenticated user
    /// </summary>
    public class UserViewModel
    {
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Token { get; set; }
    }

    /// <summary>
    /// API response wrapper for paginated claims
    /// </summary>
    public class ApiPaginatedResponse<T>
    {
        public List<T>? Claims { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
