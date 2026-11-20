using FluentValidation;
using QualityControl.Application.Features.Departments.Dtos;

namespace QualityControl.Application.Features.Departments.Validators;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Укажите код подразделения.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Укажите наименование подразделения.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ParentDepartmentId).GreaterThan(0).When(x => x.ParentDepartmentId is not null);
        RuleFor(x => x.ManagerId).GreaterThan(0).When(x => x.ManagerId is not null);
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Укажите код подразделения.").MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Укажите наименование подразделения.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ParentDepartmentId).GreaterThan(0).When(x => x.ParentDepartmentId is not null);
        RuleFor(x => x.ManagerId).GreaterThan(0).When(x => x.ManagerId is not null);
    }
}
