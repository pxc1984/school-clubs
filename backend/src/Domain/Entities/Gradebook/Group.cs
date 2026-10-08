using SchoolClubs.Domain.Common;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Domain.Entities.Gradebook;

public class Group(string title, string? description = null) : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = title;
    public string? Description { get; set; } = description;

    public IList<User> Teachers { get; set; } = [];
    public IList<Student> Students { get; set; } = [];

    public DateTimeOffset Created { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public Guid? LastModifiedBy { get; set; }
}
