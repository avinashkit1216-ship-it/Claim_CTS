namespace ClaimSubmission.API.DTOs
{
    /// <summary>
    /// DTO for creating a new claim
    /// </summary>
    public class CreateClaimRequest
    {
        public string? ClaimNumber { get; set; }
        public string? PatientName { get; set; }
        public string? ProviderName { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal ClaimAmount { get; set; }
        public string? ClaimStatus { get; set; }
    }

    /// <summary>
    /// DTO for updating an existing claim
    /// </summary>
    public class UpdateClaimRequest
    {
        public string? PatientName { get; set; }
        public string? ProviderName { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal ClaimAmount { get; set; }
        public string? ClaimStatus { get; set; }
    }

    /// <summary>
    /// DTO for claim response
    /// </summary>
    public class ClaimResponse
    {
        public int ClaimId { get; set; }
        public string? ClaimNumber { get; set; }
        public string? PatientName { get; set; }
        public string? ProviderName { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal ClaimAmount { get; set; }
        public string? ClaimStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// DTO for paginated claims list response
    /// </summary>
    public class PaginatedClaimsResponse
    {
        public List<ClaimResponse>? Claims { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
    }

    /// <summary>
    /// DTO for claims search/filter request
    /// </summary>
    public class GetClaimsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string? ClaimStatus { get; set; }
        public string? SortBy { get; set; } = "CreatedDate";
        public string? SortDirection { get; set; } = "DESC";
    }
}
