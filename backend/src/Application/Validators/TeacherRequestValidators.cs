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

public class TaskTitleRequestValidator : AbstractValidator<TaskTitleRequest>
{
    public TaskTitleRequestValidator()
    {
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
    }
}
