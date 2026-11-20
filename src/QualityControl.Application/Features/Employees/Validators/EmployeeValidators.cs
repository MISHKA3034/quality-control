using FluentValidation;
using QualityControl.Application.Features.Employees.Dtos;

namespace QualityControl.Application.Features.Employees.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.PersonnelNumber)
            .NotEmpty().WithMessage("Укажите табельный номер.")
            .MaximumLength(20);

        RuleFor(x => x.LastName).NotEmpty().WithMessage("Укажите фамилию.").MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Укажите имя.").MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.Position).NotEmpty().WithMessage("Укажите должность.").MaximumLength(150);
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Выберите подразделение.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).MaximumLength(30);

        RuleFor(x => x.HiredOn)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("Дата приёма не может быть в будущем.")
            .When(x => x.HiredOn is not null);
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.PersonnelNumber)
            .NotEmpty().WithMessage("Укажите табельный номер.")
            .MaximumLength(20);

        RuleFor(x => x.LastName).NotEmpty().WithMessage("Укажите фамилию.").MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Укажите имя.").MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.Position).NotEmpty().WithMessage("Укажите должность.").MaximumLength(150);
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Выберите подразделение.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone).MaximumLength(30);

        RuleFor(x => x.DismissedOn)
            .GreaterThanOrEqualTo(x => x.HiredOn!.Value)
            .WithMessage("Дата увольнения не может быть раньше даты приёма.")
            .When(x => x.DismissedOn is not null && x.HiredOn is not null);
    }
}
