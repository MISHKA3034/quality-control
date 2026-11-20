using FluentValidation;
using Microsoft.Extensions.Options;
using QualityControl.Application.Common.Options;
using QualityControl.Application.Features.Auth.Validators;
using QualityControl.Application.Features.Users.Dtos;

namespace QualityControl.Application.Features.Users.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Укажите имя пользователя.")
            .MinimumLength(3).WithMessage("Имя пользователя должно содержать не менее 3 символов.")
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9._-]+$")
                .WithMessage("Имя пользователя может содержать только латинские буквы, цифры и символы . _ -");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Укажите e-mail.")
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .MaximumLength(256);

        // The same policy the user faces when changing their own password.
        RuleFor(x => x.Password).ApplyPasswordPolicy(options.Value.MinPasswordLength);

        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Выберите роль.");
        RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId is not null);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Укажите e-mail.")
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .MaximumLength(256);

        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Выберите роль.");
        RuleFor(x => x.EmployeeId).GreaterThan(0).When(x => x.EmployeeId is not null);
    }
}

public class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
{
    public AdminResetPasswordRequestValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.NewPassword).ApplyPasswordPolicy(options.Value.MinPasswordLength);
    }
}
