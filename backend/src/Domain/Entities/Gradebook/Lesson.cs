using SchoolClubs.Domain.Common;

namespace SchoolClubs.Domain.Entities.Gradebook;

public class Lesson : IAuditableEntity
{
    private static readonly TimeOnly EarliestLessonTime = TimeOnly.Parse("7:00");
    private static readonly TimeOnly LatestLessonTime = TimeOnly.Parse("22:00");
    public Lesson(Group group, DateOnly date, TimeOnly? time)
    {
        Date = date;
        if (time < EarliestLessonTime || time > LatestLessonTime) throw new ArgumentException($"Time not in range {EarliestLessonTime.ToString()}-{LatestLessonTime.ToString()}");
        Time = time;

        ParentGroupId = group.Id;
        ParentGroup = group;
    }
    public Guid Id;

    public DateOnly Date;
    public TimeOnly? Time;

    public Guid ParentGroupId;
    public Group ParentGroup;

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
}
