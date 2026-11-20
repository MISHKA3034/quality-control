using FluentValidation;
using QualityControl.Application.Features.Defects.Dtos;

namespace QualityControl.Application.Features.Defects.Validators;

public class CreateDefectRequestValidator : AbstractValidator<CreateDefectRequest>
{
    public CreateDefectRequestValidator()
    {
        RuleFor(x => x.QualityInspectionId).GreaterThan(0).WithMessage("Укажите акт проверки.");
        RuleFor(x => x.DefectTypeId).GreaterThan(0).WithMessage("Выберите вид дефекта.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Опишите дефект.").MaximumLength(2000);
        RuleFor(x => x.Severity).IsInEnum().WithMessage("Укажите степень критичности.");
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Comment).MaximumLength(1000);

        RuleFor(x => x.AffectedQuantity)
            .GreaterThan(0).WithMessage("Количество дефектных изделий должно быть больше нуля.");
    }
}

public class UpdateDefectRequestValidator : AbstractValidator<UpdateDefectRequest>
{
    public UpdateDefectRequestValidator()
    {
        RuleFor(x => x.DefectTypeId).GreaterThan(0).WithMessage("Выберите вид дефекта.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Опишите дефект.").MaximumLength(2000);
        RuleFor(x => x.Severity).IsInEnum().WithMessage("Укажите степень критичности.");
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Comment).MaximumLength(1000);

        RuleFor(x => x.AffectedQuantity)
            .GreaterThan(0).WithMessage("Количество дефектных изделий должно быть больше нуля.");
    }
}
