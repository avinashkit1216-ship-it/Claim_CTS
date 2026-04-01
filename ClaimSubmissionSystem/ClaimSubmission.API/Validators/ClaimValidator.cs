// ClaimValidator.cs - Fluent Validation for Claims
using FluentValidation;
using ClaimSubmission.API.DTOs;

namespace ClaimSubmission.API.Validators
{
    /// <summary>
    /// Validator for CreateClaimRequest
    /// Ensures all claim data meets business requirements
    /// </summary>
    public class CreateClaimRequestValidator : AbstractValidator<CreateClaimRequest>
    {
        public CreateClaimRequestValidator()
        {
            RuleFor(x => x.ClaimNumber)
                .NotEmpty().WithMessage("Claim number is required")
                .Length(1, 50).WithMessage("Claim number must be 1-50 characters")
                .Matches(@"^[A-Z0-9\-]+$").WithMessage("Claim number must contain only uppercase letters, numbers, and hyphens");

            RuleFor(x => x.PatientName)
                .NotEmpty().WithMessage("Patient name is required")
                .Length(1, 100).WithMessage("Patient name must be 1-100 characters")
                .Matches(@"^[a-zA-Z\s\-'.]+$").WithMessage("Patient name contains invalid characters");

            RuleFor(x => x.ProviderName)
                .NotEmpty().WithMessage("Provider name is required")
                .Length(1, 100).WithMessage("Provider name must be 1-100 characters");

            RuleFor(x => x.DateOfService)
                .NotEmpty().WithMessage("Date of service is required")
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("Date of service cannot be in the future");

            RuleFor(x => x.ClaimAmount)
                .NotEmpty().WithMessage("Claim amount is required")
                .GreaterThan(0).WithMessage("Claim amount must be greater than $0.00")
                .LessThanOrEqualTo(999999.99M).WithMessage("Claim amount cannot exceed $999,999.99");

            RuleFor(x => x.ClaimStatus)
                .NotEmpty().WithMessage("Claim status is required")
                .Must(x => new[] { "Pending", "Approved", "Rejected", "Under Review" }.Contains(x))
                .WithMessage("Invalid claim status. Valid values: Pending, Approved, Rejected, Under Review");
        }
    }

    /// <summary>
    /// Validator for UpdateClaimRequest
    /// </summary>
    public class UpdateClaimRequestValidator : AbstractValidator<UpdateClaimRequest>
    {
        public UpdateClaimRequestValidator()
        {
            RuleFor(x => x.PatientName)
                .Length(1, 100).WithMessage("Patient name must be 1-100 characters")
                .When(x => !string.IsNullOrEmpty(x.PatientName));

            RuleFor(x => x.ProviderName)
                .Length(1, 100).WithMessage("Provider name must be 1-100 characters")
                .When(x => !string.IsNullOrEmpty(x.ProviderName));

            RuleFor(x => x.DateOfService)
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("Date of service cannot be in the future")
                .When(x => x.DateOfService != default);

            RuleFor(x => x.ClaimAmount)
                .GreaterThan(0).WithMessage("Claim amount must be greater than $0.00")
                .LessThanOrEqualTo(999999.99M).WithMessage("Claim amount cannot exceed $999,999.99");

            RuleFor(x => x.ClaimStatus)
                .Must(x => new[] { "Pending", "Approved", "Rejected", "Under Review" }.Contains(x))
                .WithMessage("Invalid claim status")
                .When(x => !string.IsNullOrEmpty(x.ClaimStatus));
        }
    }

    /// <summary>
    /// Validator for GetClaimsRequest (pagination)
    /// </summary>
    public class GetClaimsRequestValidator : AbstractValidator<DTOs.GetClaimsRequest>
    {
        public GetClaimsRequestValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Page number must be greater than 0")
                .LessThanOrEqualTo(10000).WithMessage("Page number cannot exceed 10000");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than 0")
                .LessThanOrEqualTo(500).WithMessage("Page size cannot exceed 500 records");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters")
                .When(x => !string.IsNullOrEmpty(x.SearchTerm));

            RuleFor(x => x.ClaimStatus)
                .Must(x => string.IsNullOrEmpty(x) || new[] { "Pending", "Approved", "Rejected", "Under Review" }.Contains(x))
                .WithMessage("Invalid claim status")
                .When(x => !string.IsNullOrEmpty(x.ClaimStatus));
        }
    }
}
