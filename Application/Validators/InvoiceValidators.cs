using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateInvoiceItemDtoValidator : AbstractValidator<CreateInvoiceItemDto>
{
    public CreateInvoiceItemDtoValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountPercent).InclusiveBetween(0, 100);
    }
}

public class CreateInvoiceDtoValidator : AbstractValidator<CreateInvoiceDto>
{
    public CreateInvoiceDtoValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Invoice must have at least one item");
        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemDtoValidator());
    }
}

public class UpdateInvoiceDtoValidator : AbstractValidator<UpdateInvoiceDto>
{
    public UpdateInvoiceDtoValidator()
    {
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Invoice must have at least one item");
        RuleForEach(x => x.Items).SetValidator(new CreateInvoiceItemDtoValidator());
    }
}
