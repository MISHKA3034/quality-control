using FluentValidation;
using QualityControl.Application.Features.ProductCategories.Dtos;

namespace QualityControl.Application.Features.ProductCategories.Validators;

public class CreateProductCategoryRequestValidator : AbstractValidator<CreateProductCategoryRequest>
{
    public CreateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Укажите код категории.").MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Укажите наименование категории.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId is not null);
    }
}

public class UpdateProductCategoryRequestValidator : AbstractValidator<UpdateProductCategoryRequest>
{
    public UpdateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Укажите код категории.").MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Укажите наименование категории.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ParentCategoryId).GreaterThan(0).When(x => x.ParentCategoryId is not null);
    }
}
