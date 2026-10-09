using FluentValidation;
using SchoolClubs.Application.Services.Account;
using SchoolClubs.Application.Services.Auth;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;

    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(r => r.Password).NotEmpty().MinimumLength(PasswordMinLength).MaximumLength(128);
        RuleFor(r => r.FirstName).NameRules().NotEmpty();
        RuleFor(r => r.LastName).NameRules().NotEmpty();
        RuleFor(r => r.MiddleName).NameRules();
        RuleFor(r => r.Phone).MaximumLength(32);
        RuleFor(r => r.AcceptPersonalDataPolicy)
            .Equal(true)
            .WithMessage("Нужно согласие на обработку персональных данных.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().MaximumLength(254);
        RuleFor(r => r.Password).NotEmpty().MaximumLength(128);
    }
}

public class AcceptConsentRequestValidator : AbstractValidator<AcceptConsentRequest>
{
    public AcceptConsentRequestValidator()
    {
        RuleFor(r => r.Accept)
            .Equal(true)
            .WithMessage("Нужно согласие на обработку персональных данных.");
    }
}

public class ChangeUsernameRequestValidator : AbstractValidator<ChangeUsernameRequest>
{
    public ChangeUsernameRequestValidator()
    {
        RuleFor(r => r.Username)
            .Must(username => string.IsNullOrWhiteSpace(username)
                              || User.IsValidUsername(User.NormalizeUsername(username)))
            .WithMessage($"От {User.UsernameMinLength} до {User.UsernameMaxLength} символов: латиница, цифры, "
                         + "«_», «.», «-»; начинается с буквы или цифры.");
    }
}

internal static class NameRuleExtensions
{
    /// <summary>Части ФИО хранятся по отдельности и не содержат пробелов (см. Profile.Rename).</summary>
    public static IRuleBuilderOptions<T, string?> NameRules<T>(this IRuleBuilder<T, string?> rule)
    {
        return rule
            .MaximumLength(100)
            .Must(name => name is null || !name.Trim().Any(char.IsWhiteSpace))
            .WithMessage("Без пробелов: фамилия, имя и отчество вводятся в отдельные поля.");
    }
}
