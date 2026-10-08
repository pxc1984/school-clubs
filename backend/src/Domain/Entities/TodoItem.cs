using SchoolClubs.Domain.Common;

namespace SchoolClubs.Domain.Entities;

public class TodoItem : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }

    public void Toggle()
    {
        IsDone = !IsDone;
    }
}
