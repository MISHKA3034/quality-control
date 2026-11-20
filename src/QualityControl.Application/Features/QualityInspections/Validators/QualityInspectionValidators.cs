using FluentValidation;
using QualityControl.Application.Features.QualityInspections.Dtos;

namespace QualityControl.Application.Features.QualityInspections.Validators;

public class CreateInspectionResultRequestValidator : AbstractValidator<CreateInspectionResultRequest>
{
    public CreateInspectionResultRequestValidator()
    {
        RuleFor(x => x.ParameterName)
            .NotEmpty().WithMessage("Укажите контролируемый параметр.")
            .MaximumLength(200);

        RuleFor(x => x.ExpectedValue).MaximumLength(200);
        RuleFor(x => x.ActualValue).MaximumLength(200);
        RuleFor(x => x.UnitOfMeasure).MaximumLength(20);
        RuleFor(x => x.Outcome).IsInEnum().WithMessage("Укажите результат по параметру.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class CreateInspectionDefectRequestValidator : AbstractValidator<CreateInspectionDefectRequest>
{
    public CreateInspectionDefectRequestValidator()
    {
        RuleFor(x => x.DefectTypeId).GreaterThan(0).WithMessage("Выберите вид дефекта.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Опишите дефект.")
            .MaximumLength(2000);

        RuleFor(x => x.Severity).IsInEnum().WithMessage("Укажите степень критичности.");
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Comment).MaximumLength(1000);

        RuleFor(x => x.AffectedQuantity)
            .GreaterThan(0).WithMessage("Количество дефектных изделий должно быть больше нуля.");
    }
}

public class CreateQualityInspectionRequestValidator : AbstractValidator<CreateQualityInspectionRequest>
{
    public CreateQualityInspectionRequestValidator()
    {
        RuleFor(x => x.ProductionBatchId).GreaterThan(0).WithMessage("Выберите партию.");
        RuleFor(x => x.Outcome).IsInEnum().WithMessage("Укажите результат проверки.");
        RuleFor(x => x.Comment).MaximumLength(2000);

        RuleFor(x => x.InspectionDate)
            .NotEmpty().WithMessage("Укажите дату проверки.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
                .WithMessage("Дата проверки не может быть в будущем.");

        RuleFor(x => x.QuantityInspected)
            .GreaterThan(0).WithMessage("Укажите количество проверенных изделий.");

        RuleFor(x => x.QuantityAccepted)
            .GreaterThanOrEqualTo(0).WithMessage("Количество принятых изделий не может быть отрицательным.");

        RuleFor(x => x.QuantityRejected)
            .GreaterThanOrEqualTo(0).WithMessage("Количество забракованных изделий не может быть отрицательным.");

        // Checked here for a friendly field-level message; the entity re-checks it
        // against the real batch size, which the request cannot know.
        RuleFor(x => x)
            .Must(x => x.QuantityAccepted + x.QuantityRejected == x.QuantityInspected)
            .WithName(nameof(CreateQualityInspectionRequest.QuantityInspected))
            .WithMessage("Сумма принятых и забракованных изделий должна равняться количеству проверенных.");

        RuleFor(x => x.Results).NotNull();
        RuleForEach(x => x.Results).SetValidator(new CreateInspectionResultRequestValidator());

        RuleFor(x => x.Defects).NotNull();
        RuleForEach(x => x.Defects).SetValidator(new CreateInspectionDefectRequestValidator());
    }
}

public class UpdateQualityInspectionRequestValidator : AbstractValidator<UpdateQualityInspectionRequest>
{
    public UpdateQualityInspectionRequestValidator()
    {
        RuleFor(x => x.InspectionDate)
            .NotEmpty().WithMessage("Укажите дату проверки.")
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
                .WithMessage("Дата проверки не может быть в будущем.");

        RuleFor(x => x.Comment).MaximumLength(2000);
        RuleFor(x => x.Results).NotNull();
        RuleForEach(x => x.Results).SetValidator(new CreateInspectionResultRequestValidator());
    }
}
