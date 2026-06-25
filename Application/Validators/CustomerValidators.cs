using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.GSTIN).Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            .When(x => !string.IsNullOrEmpty(x.GSTIN))
            .WithMessage("Invalid GSTIN format.");
        RuleFor(x => x.BillingAddress).NotNull();
    }
}

public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.GSTIN).Matches(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$")
            .When(x => !string.IsNullOrEmpty(x.GSTIN))
            .WithMessage("Invalid GSTIN format.");
        RuleFor(x => x.BillingAddress).NotNull();
    }
}
