using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Data;

namespace SchoolClubs.Domain.Entities.Gradebook;

public class LessonGradebook(Lesson lesson, DataEntry? dataEntry = null) : IAuditableEntity
{
    public Guid Id;

    public Guid LessonId = lesson.Id;
    public Lesson Lesson = lesson;

    public Guid? DataEntryId = dataEntry?.Id;
    public DataEntry? StoredFile = dataEntry;

    public IList<GradebookEntry> GradebookEntries = [];
    // Sometime later implement proper and optimised guarding of domain model
    // {
    //     get;
    //     set
    //     {
    //         UniqueListElementsException.ThrowIfHasDuplicates(value, v => (v.LessonId, v.StudentId, v.TaskId));
    //         _gradebookEntries = value;
    //     }
    // } = [];

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
}
