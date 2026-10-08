using FluentValidation;
using SchoolClubs.Application.Services.Admin;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Validators;

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(2000);
    }
}

public class UpdateGroupRequestValidator : AbstractValidator<UpdateGroupRequest>
{
    public UpdateGroupRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(2000);
    }
}

public class NewStudentRequestValidator : AbstractValidator<NewStudentRequest>
{
    public NewStudentRequestValidator()
    {
        RuleFor(r => r.LastName).NameRules().NotEmpty();
        RuleFor(r => r.FirstName).NameRules().NotEmpty();
        RuleFor(r => r.MiddleName).NameRules();
        RuleFor(r => r.Grade).MaximumLength(Student.GradeMaxLength);
    }
}

public class AddGroupStudentsRequestValidator : AbstractValidator<AddGroupStudentsRequest>
{
    public AddGroupStudentsRequestValidator()
    {
        RuleFor(r => r)
            .Must(r => (r.StudentIds?.Count ?? 0) + (r.NewStudents?.Count ?? 0) > 0)
            .WithMessage("Нужно указать хотя бы одного ученика.");
        RuleFor(r => r.StudentIds).Must(ids => ids == null || ids.Count <= 200);
        RuleFor(r => r.NewStudents).Must(list => list == null || list.Count <= StudentNameParser.MaxLines);
        RuleForEach(r => r.NewStudents).SetValidator(new NewStudentRequestValidator());
    }
}

public class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(r => r.LastName).NameRules().NotEmpty();
        RuleFor(r => r.FirstName).NameRules().NotEmpty();
        RuleFor(r => r.MiddleName).NameRules();
        RuleFor(r => r.Grade).MaximumLength(Student.GradeMaxLength);
    }
}

public class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(r => r.LastName).NameRules().NotEmpty();
        RuleFor(r => r.FirstName).NameRules().NotEmpty();
        RuleFor(r => r.MiddleName).NameRules();
        RuleFor(r => r.Grade).MaximumLength(Student.GradeMaxLength);
    }
}

public class ParseNamesRequestValidator : AbstractValidator<ParseNamesRequest>
{
    public ParseNamesRequestValidator()
    {
        RuleFor(r => r.Text).NotEmpty().MaximumLength(20_000);
    }
}

public class SetRolesRequestValidator : AbstractValidator<SetRolesRequest>
{
    public SetRolesRequestValidator()
    {
        RuleFor(r => r.Roles).NotNull();
        RuleForEach(r => r.Roles).IsInEnum();
        RuleFor(r => r.Roles)
            .Must(roles => roles == null || roles.Distinct().Count() == roles.Count)
            .WithMessage("Роли не должны повторяться.");
    }
}

public class InviteTeacherRequestValidator : AbstractValidator<InviteTeacherRequest>
{
    public InviteTeacherRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(r => r.LastName).NameRules().NotEmpty();
        RuleFor(r => r.FirstName).NameRules().NotEmpty();
        RuleFor(r => r.MiddleName).NameRules();
    }
}
