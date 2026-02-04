using FluentValidation;
using SuperAdminDashboard.Application.Features.Tenants.DTOs;

namespace SuperAdminDashboard.Application.Features.Tenants.Validators;

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    public CreateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required")
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Domain)
            .MaximumLength(255).WithMessage("Domain cannot exceed 255 characters")
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9-\.]+\.[a-zA-Z]{2,}$")
            .When(x => !string.IsNullOrEmpty(x.Domain))
            .WithMessage("Invalid domain format");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Invalid email format");
    }
}

public class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    public UpdateTenantRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Slug)
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug can only contain lowercase letters, numbers, and hyphens")
            .When(x => !string.IsNullOrEmpty(x.Slug));

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Invalid email format");
    }
}
