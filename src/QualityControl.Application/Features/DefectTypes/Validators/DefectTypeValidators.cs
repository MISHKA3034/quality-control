using FluentValidation;
using QualityControl.Application.Features.DefectTypes.Dtos;

namespace QualityControl.Application.Features.DefectTypes.Validators;

public class CreateDefectTypeRequestValidator : AbstractValidator<CreateDefectTypeRequest>
{
    public CreateDefectTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Укажите код вида дефекта.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Укажите наименование вида дефекта.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DefaultSeverity).IsInEnum().WithMessage("Укажите степень критичности по умолчанию.");
    }
}

public class UpdateDefectTypeRequestValidator : AbstractValidator<UpdateDefectTypeRequest>
{
    public UpdateDefectTypeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Укажите код вида дефекта.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Укажите наименование вида дефекта.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.DefaultSeverity).IsInEnum().WithMessage("Укажите степень критичности по умолчанию.");
    }
}
