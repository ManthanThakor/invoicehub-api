using Application.DTOs;
using FluentValidation;

namespace Application.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GSTRate).InclusiveBetween(0, 28);
    }
}

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GSTRate).InclusiveBetween(0, 28);
    }
}

public class StockAdjustmentDtoValidator : AbstractValidator<StockAdjustmentDto>
{
    public StockAdjustmentDtoValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.MovementType).IsInEnum();
    }
}
