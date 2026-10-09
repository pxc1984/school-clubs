using FluentValidation;
using SchoolClubs.Application.Services.Gradebook;
using SchoolClubs.Application.Services.Lessons;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Validators;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(r => r.Topic).MaximumLength(Lesson.TopicMaxLength);
        RuleFor(r => r.TaskCount).InclusiveBetween(0, LessonService.MaxTasks);
    }
}

public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
{
    public UpdateLessonRequestValidator()
    {
        RuleFor(r => r.Topic).MaximumLength(Lesson.TopicMaxLength);
    }
}

public class TaskRequestValidator : AbstractValidator<TaskRequest>
{
    public TaskRequestValidator()
    {
        RuleFor(r => r.Number)
            .Must(n => n == null || TaskNumber.IsValid(n.Trim()))
            .WithMessage($"Номер задачи — до {TaskNumber.MaxLength} символов, без пробелов и двоеточий.");
        RuleFor(r => r.Title).MaximumLength(Domain.Entities.Gradebook.Task.TitleMaxLength);
    }
}

public class SaveGradebookRequestValidator : AbstractValidator<SaveGradebookRequest>
{
    /// <summary>Группа до ~30 учеников × до 50 задач — с запасом.</summary>
    public const int MaxChanges = 5000;

    public SaveGradebookRequestValidator()
    {
        RuleFor(r => r.Changes).NotNull().Must(c => c == null || c.Count <= MaxChanges);
        RuleForEach(r => r.Changes).ChildRules(change =>
        {
            change.RuleFor(c => c.Status).IsInEnum();
            change.RuleFor(c => c.Comment).MaximumLength(GradebookEntry.CommentMaxLength);
        });
        RuleFor(r => r.Changes)
            .Must(c => c == null || c.Select(x => (x.StudentId, x.TaskId)).Distinct().Count() == c.Count)
            .WithMessage("Каждая ячейка — не больше одного раза.");

        RuleFor(r => r.Attendance).Must(a => a == null || a.Count <= MaxChanges);
        RuleForEach(r => r.Attendance).ChildRules(change => change.RuleFor(c => c.Status).IsInEnum());
        RuleFor(r => r.Attendance)
            .Must(a => a == null || a.Select(x => x.StudentId).Distinct().Count() == a.Count)
            .WithMessage("Посещаемость каждого ученика — не больше одного раза.");
    }
}
