using System;
using System.ComponentModel.DataAnnotations;

namespace ClaimSubmission.Web.Models {

    public class ClaimViewListModel {

        public int ClaimId { get; set; }
        public string? ClaimNumber { get; set; }
        public string? PatientName { get; set; }
        public string? ProviderName { get; set; }
        public DateTime DateOfService { get; set; }
        public decimal ClaimAmount { get; set; }
        public string? ClaimStatus { get; set; }

    }
     
    public class LoginViewModel {
        [Required(ErrorMessage = "UserName is Mandatory !")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Password is Mandatory !")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }


    public class AddClaimViewModel {

        [Required(ErrorMessage = "Claim Number is Mandatory!")]
        [StringLength(50)]
        public string? ClaimNumber { get; set; }

        [Required(ErrorMessage = "Patient Name is mandatory.")]
        [StringLength(100)]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Provider Name is Mandatory !")]
        [StringLength(100)]
        public string? ProviderName { get; set; }

        [Required(ErrorMessage = "Date of Service is mandatory.")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DateOfService { get; set; }

        [Required(ErrorMessage = "Claim Amount is Mandatory !")]
        [Range(0.01, 99999999.99)]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim Status is Mandatory !")]
        [StringLength(50)]
        public string? ClaimStatus { get; set; }
    }

    public class EditClaimViewModel {

        public int ClaimId { get; set; }

        [Required]
        [StringLength(50)]
        public string? ClaimNumber { get; set; }

        [Required(ErrorMessage = "Patient Name is Mandatory !")]
        [StringLength(100)]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Provider Name is Mandatory !")]
        [StringLength(100)]
        public string? ProviderName { get; set; }

        [Required(ErrorMessage = "Date of Service is mandatory.")]
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime DateOfService { get; set; }

        [Required(ErrorMessage = "Claim Amount is Mandatory !")]
        [Range(0.01, 9999999.99)]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Claim Status is mandatory.")]
        [StringLength(50)]
        public string? ClaimStatus { get; set; }
    }

    public class UserViewModel {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? Token { get; set; }
    }


}