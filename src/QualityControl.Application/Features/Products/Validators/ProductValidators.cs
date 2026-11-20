using FluentValidation;
using QualityControl.Application.Features.Products.Dtos;

namespace QualityControl.Application.Features.Products.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Code).ApplyProductCodeRules();
        RuleFor(x => x.Name).ApplyProductNameRules();
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Выберите категорию изделия.");
        RuleFor(x => x.UnitOfMeasure).ApplyUnitOfMeasureRules();
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DrawingNumber).MaximumLength(100);
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Code).ApplyProductCodeRules();
        RuleFor(x => x.Name).ApplyProductNameRules();
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Выберите категорию изделия.");
        RuleFor(x => x.UnitOfMeasure).ApplyUnitOfMeasureRules();
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DrawingNumber).MaximumLength(100);
    }
}

/// <summary>
/// Field rules shared by create and update, so the two cannot drift apart.
/// </summary>
internal static class ProductRules
{
    public static IRuleBuilderOptions<T, string> ApplyProductCodeRules<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Укажите код изделия.")
            .MaximumLength(50).WithMessage("Код изделия не должен превышать 50 символов.")
            .Matches(@"^[A-Za-zА-Яа-я0-9._\-/]+$")
                .WithMessage("Код изделия может содержать только буквы, цифры и символы . _ - /");

    public static IRuleBuilderOptions<T, string> ApplyProductNameRules<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Укажите наименование изделия.")
            .MaximumLength(300).WithMessage("Наименование не должно превышать 300 символов.");

    public static IRuleBuilderOptions<T, string> ApplyUnitOfMeasureRules<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Укажите единицу измерения.")
            .MaximumLength(20);
}
