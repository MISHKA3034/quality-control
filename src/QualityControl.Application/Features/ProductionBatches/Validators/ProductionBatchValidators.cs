using FluentValidation;
using QualityControl.Application.Features.ProductionBatches.Dtos;

namespace QualityControl.Application.Features.ProductionBatches.Validators;

public class CreateProductionBatchRequestValidator : AbstractValidator<CreateProductionBatchRequest>
{
    public CreateProductionBatchRequestValidator()
    {
        RuleFor(x => x.BatchNumber).ApplyBatchNumberRules();
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Выберите изделие.");
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Выберите участок.");
        RuleFor(x => x.ForemanId).GreaterThan(0).WithMessage("Выберите ответственного мастера.");
        RuleFor(x => x.Quantity).ApplyQuantityRules();
        RuleFor(x => x.ManufactureDate).ApplyManufactureDateRules();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateProductionBatchRequestValidator : AbstractValidator<UpdateProductionBatchRequest>
{
    public UpdateProductionBatchRequestValidator()
    {
        RuleFor(x => x.BatchNumber).ApplyBatchNumberRules();
        RuleFor(x => x.ProductId).GreaterThan(0).WithMessage("Выберите изделие.");
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Выберите участок.");
        RuleFor(x => x.ForemanId).GreaterThan(0).WithMessage("Выберите ответственного мастера.");
        RuleFor(x => x.Quantity).ApplyQuantityRules();
        RuleFor(x => x.ManufactureDate).ApplyManufactureDateRules();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class ChangeBatchStatusRequestValidator : AbstractValidator<ChangeBatchStatusRequest>
{
    public ChangeBatchStatusRequestValidator()
    {
        // Only that the value is a defined enum member. Whether this particular move
        // is legal is the domain's call, not the validator's.
        RuleFor(x => x.Status).IsInEnum().WithMessage("Указан неизвестный статус партии.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

internal static class ProductionBatchRules
{
    public static IRuleBuilderOptions<T, string> ApplyBatchNumberRules<T>(this IRuleBuilder<T, string> rule) =>
        rule
            .NotEmpty().WithMessage("Укажите номер партии.")
            .MaximumLength(50).WithMessage("Номер партии не должен превышать 50 символов.");

    public static IRuleBuilderOptions<T, int> ApplyQuantityRules<T>(this IRuleBuilder<T, int> rule) =>
        rule
            .GreaterThan(0).WithMessage("Количество изделий должно быть больше нуля.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Количество изделий указано некорректно.");

    public static IRuleBuilderOptions<T, DateTime> ApplyManufactureDateRules<T>(
        this IRuleBuilder<T, DateTime> rule) =>
        rule
            .NotEmpty().WithMessage("Укажите дату изготовления.")

            // Allow one day of slack so a client in a timezone ahead of the server
            // is not rejected for entering "today".
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Дата изготовления не может быть в будущем.")
            .GreaterThan(new DateTime(2000, 1, 1))
                .WithMessage("Дата изготовления указана некорректно.");
}
