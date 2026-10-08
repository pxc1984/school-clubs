using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Common.Interfaces;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Common;

/// <summary>
/// Проверка владения для раздела учителя: группа должна быть в <c>LinkedGroups</c> текущего пользователя.
/// Несуществующий id — 404, чужая группа или занятие — 403.
/// </summary>
public class TeacherAccess(ICurrentUser currentUser, IGroupRepository groups, ILessonRepository lessons)
{
    /// <summary>Группа учителя вместе с учителями и учениками.</summary>
    public async Task<Group> GetGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groups.GetWithMembersAsync(groupId, cancellationToken);
        Guard.Against.NotFound(groupId, group);
        if (currentUser.UserId is not { } userId || group.Teachers.All(t => t.Id != userId))
            throw new ForbiddenAccessException();

        return group;
    }

    /// <summary>Занятие (с задачами) из группы учителя и сама группа.</summary>
    public async Task<(Lesson Lesson, Group Group)> GetLessonAsync(Guid lessonId,
        CancellationToken cancellationToken)
    {
        var lesson = await lessons.GetWithTasksAsync(lessonId, cancellationToken);
        Guard.Against.NotFound(lessonId, lesson);
        var group = await GetGroupAsync(lesson.ParentGroupId, cancellationToken);
        return (lesson, group);
    }
}
