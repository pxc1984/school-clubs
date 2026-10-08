namespace SchoolClubs.Domain.Entities.Gradebook;

public class Task(Lesson lesson, long order)
{
    public Guid Id { get; set; }
    public long Order = order;

    public IList<GradebookEntry> Entries = [];

    public Guid LessonId = lesson.Id;
    public Lesson Lesson = lesson;
}
