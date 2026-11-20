using FluentValidation;
using Microsoft.Extensions.Options;
using QualityControl.Application.Common.Options;
using QualityControl.Application.Features.Auth.Dtos;

namespace QualityControl.Application.Features.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Укажите имя пользователя или e-mail.")
            .MaximumLength(256);

        // No complexity rules on sign-in: the stored password must be accepted as
        // it is, whatever policy was in force when it was set.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Укажите пароль.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Токен обновления обязателен.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Укажите e-mail.")
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .MaximumLength(256);
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress().WithMessage("Некорректный формат e-mail.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Токен восстановления обязателен.");

        RuleFor(x => x.NewPassword)
            .ApplyPasswordPolicy(options.Value.MinPasswordLength);
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Укажите текущий пароль.");

        RuleFor(x => x.NewPassword)
            .ApplyPasswordPolicy(options.Value.MinPasswordLength)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("Новый пароль должен отличаться от текущего.");
    }
}

/// <summary>
/// One definition of the password policy, shared by every rule that sets a password.
/// </summary>
public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordPolicy<T>(
        this IRuleBuilder<T, string> rule,
        int minLength) =>
        rule
            .NotEmpty().WithMessage("Укажите пароль.")
            .MinimumLength(minLength)
                .WithMessage($"Пароль должен содержать не менее {minLength} символов.")
            .MaximumLength(128)
                .WithMessage("Пароль не должен превышать 128 символов.")
            .Must(p => p.Any(char.IsDigit))
                .WithMessage("Пароль должен содержать хотя бы одну цифру.")
            .Must(p => p.Any(char.IsUpper))
                .WithMessage("Пароль должен содержать хотя бы одну заглавную букву.")
            .Must(p => p.Any(char.IsLower))
                .WithMessage("Пароль должен содержать хотя бы одну строчную букву.");
}
