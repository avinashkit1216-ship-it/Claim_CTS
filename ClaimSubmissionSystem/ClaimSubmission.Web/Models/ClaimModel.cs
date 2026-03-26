using System;
using System.ComponentModel.DataAnnotations;

namespace ClaimSubmission.Web.Models
{
    /// <summary>
    /// View model for displaying claims in a list
    /// </summary>
    public class ClaimViewListModel
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
    /// View model for paginated claims list
    /// </summary>
    public class ClaimsPaginatedListViewModel
    {
        public List<ClaimViewListModel>? Claims { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalRecords + PageSize - 1) / PageSize;
        
        // For filtering and searching
        public string? SearchTerm { get; set; }
        public string? ClaimStatus { get; set; }
        public string? SortBy { get; set; } = "CreatedDate";
        public string? SortDirection { get; set; } = "DESC";
    }

    /// <summary>
    /// View model for creating a new claim
    /// </summary>
    public class CreateClaimViewModel
    {
        [Required(ErrorMessage = "Claim Number is mandatory")]
        [StringLength(50, ErrorMessage = "Claim Number cannot be longer than 50 characters")]
        public string? ClaimNumber { get; set; }

        [Required(ErrorMessage = "Patient Name is mandatory")]
        [StringLength(100, ErrorMessage = "Patient Name cannot be longer than 100 characters")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Provider Name is mandatory")]
        [StringLength(100, ErrorMessage = "Provider Name cannot be longer than 100 characters")]
        public string? ProviderName { get; set; }

        [Required(ErrorMessage = "Date of Service is mandatory")]
        [DataType(DataType.Date)]
        public DateTime DateOfService { get; set; }

        [Required(ErrorMessage = "Claim Amount is mandatory")]
        [Range(0.01, 999999.99, ErrorMessage = "Claim Amount must be between 0.01 and 999999.99")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim Status is mandatory")]
        [StringLength(50, ErrorMessage = "Claim Status cannot be longer than 50 characters")]
        public string? ClaimStatus { get; set; }
    }

    /// <summary>
    /// View model for editing an existing claim
    /// </summary>
    public class EditClaimViewModel
    {
        [Required]
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Patient Name is mandatory")]
        [StringLength(100, ErrorMessage = "Patient Name cannot be longer than 100 characters")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Provider Name is mandatory")]
        [StringLength(100, ErrorMessage = "Provider Name cannot be longer than 100 characters")]
        public string? ProviderName { get; set; }

        [Required(ErrorMessage = "Date of Service is mandatory")]
        [DataType(DataType.Date)]
        public DateTime DateOfService { get; set; }

        [Required(ErrorMessage = "Claim Amount is mandatory")]
        [Range(0.01, 999999.99, ErrorMessage = "Claim Amount must be between 0.01 and 999999.99")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim Status is mandatory")]
        [StringLength(50, ErrorMessage = "Claim Status cannot be longer than 50 characters")]
        public string? ClaimStatus { get; set; }

        // For display
        public string? ClaimNumber { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// View model for login
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is mandatory")]
        [StringLength(100, ErrorMessage = "Username cannot be longer than 100 characters")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is mandatory")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// Add Claim View Model (legacy, maintained for backward compatibility)
    /// </summary>
    public class AddClaimViewModel
    {
        [Required(ErrorMessage = "Claim Number is mandatory")]
        [StringLength(50, ErrorMessage = "Claim Number cannot be longer than 50 characters")]
        public string? ClaimNumber { get; set; }

        [Required(ErrorMessage = "Patient Name is mandatory")]
        [StringLength(100, ErrorMessage = "Patient Name cannot be longer than 100 characters")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Provider Name is mandatory")]
        [StringLength(100, ErrorMessage = "Provider Name cannot be longer than 100 characters")]
        public string? ProviderName { get; set; }

        [Required(ErrorMessage = "Date of Service is mandatory")]
        [DataType(DataType.Date)]
        public DateTime DateOfService { get; set; }

        [Required(ErrorMessage = "Claim Amount is mandatory")]
        [Range(0.01, 999999.99, ErrorMessage = "Claim Amount must be between 0.01 and 999999.99")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim Status is mandatory")]
        [StringLength(50, ErrorMessage = "Claim Status cannot be longer than 50 characters")]
        public string? ClaimStatus { get; set; }
    }
}


