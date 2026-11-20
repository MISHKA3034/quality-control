using FluentValidation;
using QualityControl.Application.Features.CorrectiveActions.Dtos;

namespace QualityControl.Application.Features.CorrectiveActions.Validators;

public class CreateCorrectiveActionRequestValidator : AbstractValidator<CreateCorrectiveActionRequest>
{
    public CreateCorrectiveActionRequestValidator()
    {
        RuleFor(x => x.DefectId).GreaterThan(0).WithMessage("Укажите дефект.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Опишите корректирующее действие.")
            .MaximumLength(2000);

        RuleFor(x => x.AssignedToId).GreaterThan(0).When(x => x.AssignedToId is not null);

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(_ => DateTime.UtcNow.Date)
            .WithMessage("Срок исполнения не может быть в прошлом.")
            .When(x => x.DueDate is not null);
    }
}

public class UpdateCorrectiveActionRequestValidator : AbstractValidator<UpdateCorrectiveActionRequest>
{
    public UpdateCorrectiveActionRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Опишите корректирующее действие.")
            .MaximumLength(2000);

        RuleFor(x => x.ResolutionNotes).MaximumLength(2000);

        // No past-date check on update: an existing action may legitimately have a
        // due date that has already gone by, and re-saving it must not fail.
    }
}

public class AssignCorrectiveActionRequestValidator : AbstractValidator<AssignCorrectiveActionRequest>
{
    public AssignCorrectiveActionRequestValidator()
    {
        RuleFor(x => x.AssignedToId).GreaterThan(0).WithMessage("Выберите ответственного сотрудника.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class ChangeCorrectiveActionStatusRequestValidator : AbstractValidator<ChangeCorrectiveActionStatusRequest>
{
    public ChangeCorrectiveActionStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Указан неизвестный статус.");
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
