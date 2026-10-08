using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;

namespace SchoolClubs.Application.Services.Admin;

internal static class AdminMapping
{
    public static UserRefDto ToRef(this User user)
    {
        return new UserRefDto(user.Id, user.LastName, user.FirstName, user.MiddleName);
    }

    public static StudentDto ToDto(this Student student)
    {
        return new StudentDto(student.Id, student.LastName, student.FirstName, student.MiddleName, student.Grade);
    }

    public static GroupRefDto ToRef(this Group group)
    {
        return new GroupRefDto(group.Id, group.Title, group.IsArchived);
    }

    public static IReadOnlyList<StudentDto> ToDtos(this IEnumerable<Student> students)
    {
        return students.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).Select(ToDto).ToList();
    }

    public static IReadOnlyList<UserRefDto> ToRefs(this IEnumerable<User> users)
    {
        return users.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).Select(ToRef).ToList();
    }

    public static IReadOnlyList<GroupRefDto> ToRefs(this IEnumerable<Group> groups)
    {
        return groups.OrderBy(g => g.Title).Select(ToRef).ToList();
    }

    public static Student ToStudent(this NewStudentRequest request)
    {
        return new Student(request.FirstName.Trim(), NullIfBlank(request.MiddleName), request.LastName.Trim(),
            request.Grade);
    }

    public static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
