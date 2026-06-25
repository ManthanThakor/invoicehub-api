using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class RecordPaymentDtoValidator : AbstractValidator<RecordPaymentDto>
{
    public RecordPaymentDtoValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentDate).NotEmpty();
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x).Must(x => x.InvoiceId.HasValue || x.PurchaseOrderId.HasValue)
            .WithMessage("Either InvoiceId or PurchaseOrderId must be provided.");
    }
}
