using FluentValidation;
using SchoolClubs.Application.Services.AccessRequests;
using SchoolClubs.Domain.Entities.Access;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Validators;

public class CreateAccessRequestRequestValidator : AbstractValidator<CreateAccessRequestRequest>
{
    public CreateAccessRequestRequestValidator()
    {
        RuleFor(r => r.GroupId).NotEmpty();
        RuleFor(r => r.ChildLastName).NameRules().NotEmpty();
        RuleFor(r => r.ChildFirstName).NameRules().NotEmpty();
        RuleFor(r => r.ChildMiddleName).NameRules();
        RuleFor(r => r.Grade).MaximumLength(Student.GradeMaxLength);
        RuleFor(r => r.Relation).IsInEnum();
        RuleFor(r => r.Comment).MaximumLength(AccessRequest.CommentMaxLength);
        RuleFor(r => r.AcceptChildDataConsent)
            .Equal(true)
            .WithMessage("Нужно подтвердить, что вы законный представитель, и дать согласие на обработку данных ребёнка.");
    }
}

public class ApproveAccessRequestRequestValidator : AbstractValidator<ApproveAccessRequestRequest>
{
    public ApproveAccessRequestRequestValidator()
    {
        RuleFor(r => r)
            .Must(r => (r.StudentId is null) != (r.NewStudent is null))
            .WithMessage("Укажите ровно одно: существующего ученика или нового.");
        RuleFor(r => r.NewStudent!).SetValidator(new NewStudentRequestValidator()).When(r => r.NewStudent is not null);
    }
}

public class RejectAccessRequestRequestValidator : AbstractValidator<RejectAccessRequestRequest>
{
    public RejectAccessRequestRequestValidator()
    {
        RuleFor(r => r.Reason).NotEmpty().MaximumLength(AccessRequest.RejectReasonMaxLength);
    }
}
