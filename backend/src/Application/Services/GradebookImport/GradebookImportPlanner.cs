using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Entities.Gradebook;
using SchoolClubs.Domain.Entities.Users;
using Group = SchoolClubs.Domain.Entities.Gradebook.Group;

namespace SchoolClubs.Application.Services.GradebookImport;

/// <summary>Сопоставление разобранного листа с занятием: какие отметки добавятся, изменятся, что не распознано.</summary>
public record ImportPlan(
    bool ForeignFile,
    Guid? OtherLessonId,
    bool LessonMismatch,
    IReadOnlyList<ImportChangeDto> Changes,
    IReadOnlyList<UnrecognizedRowDto> UnrecognizedRows,
    IReadOnlyList<StudentDto> MissingStudents,
    IReadOnlyList<string> MissingTaskNumbers,
    IReadOnlyList<string> CellErrors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

/// <summary>
/// Чистая логика импорта (без файлов и БД): задачи — по скрытому id, иначе по номеру «Задача N» (без учёта регистра);
/// ученики — по скрытому id, иначе по нормализованному ФИО среди учеников группы.
/// Пустая ячейка — «не изменять»: импорт только добавляет и перезаписывает.
/// </summary>
public static class GradebookImportPlanner
{
    public static ImportPlan Plan(ParsedGradebook file,
        Lesson lesson,
        Group group,
        LessonGradebook? gradebook,
        IReadOnlyDictionary<int, Guid?>? rowMappings = null)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var cellErrors = new List<string>();

        var foreign = file.Meta is null;
        var sameLesson = file.Meta?.LessonId is null || file.Meta.LessonId == lesson.Id;
        var sameGroup = file.Meta?.GroupId is null || file.Meta.GroupId == group.Id;
        var mismatch = !foreign && (!sameLesson || !sameGroup);

        // Колонки → задачи.
        var tasksById = lesson.Tasks.ToDictionary(t => t.Id);
        var columns = new Dictionary<string, (Domain.Entities.Gradebook.Task? Task, string Number)>();
        var missingNumbers = new List<string>();
        foreach (var column in file.Columns)
        {
            if (column.TaskId is { } taskId && tasksById.TryGetValue(taskId, out var byId))
            {
                columns[column.Letter] = (byId, byId.Number);
                continue;
            }

            if (column.TaskId is not null && sameLesson && !foreign)
            {
                warnings.Add($"Колонка {column.Letter} («{column.Header}»): задача удалена в системе, колонка пропущена.");
                continue;
            }

            if (column.Number is not { } number)
            {
                warnings.Add($"Колонка {column.Letter} («{column.Header}») не распознана и пропущена.");
                continue;
            }

            if (lesson.FindTaskByNumber(number) is { } byNumber)
            {
                columns[column.Letter] = (byNumber, byNumber.Number);
            }
            else
            {
                columns[column.Letter] = (null, number);
                if (!missingNumbers.Any(n => TaskNumber.AreSame(n, number)))
                    missingNumbers.Add(number);
            }
        }

        // Строки → ученики.
        var existing = gradebook?.GradebookEntries.ToDictionary(e => (e.StudentId, e.TaskId)) ?? [];
        var changes = new List<ImportChangeDto>();
        var unrecognized = new List<UnrecognizedRowDto>();
        var rowsByStudent = new Dictionary<Guid, int>();

        foreach (var row in file.Rows)
        {
            Student? student;
            if (rowMappings is not null && rowMappings.TryGetValue(row.Row, out var mapped))
            {
                if (mapped is null)
                    continue;

                student = group.Students.FirstOrDefault(s => s.Id == mapped)
                          ?? throw new ArgumentException($"student {mapped} for row {row.Row} isn't in the group");
            }
            else
            {
                var (found, reason, candidates) = Resolve(row, group);
                if (found is null)
                {
                    unrecognized.Add(new UnrecognizedRowDto(row.Row, row.Name, reason!, candidates.ToDtos()));
                    continue;
                }

                student = found;
            }

            if (rowsByStudent.TryGetValue(student.Id, out var firstRow))
            {
                errors.Add($"{student.LastName} {student.FirstName} встречается в строках {firstRow} и {row.Row}.");
                continue;
            }

            rowsByStudent[student.Id] = row.Row;

            foreach (var cell in row.Cells)
            {
                if (!columns.TryGetValue(cell.ColumnLetter, out var target))
                    continue;
                if (!GradebookSymbols.TryParse(cell.Text, out var status))
                {
                    cellErrors.Add($"{cell.Address}: «{cell.Text}»");
                    continue;
                }

                if (target.Task is null)
                {
                    changes.Add(new ImportChangeDto(row.Row, student.Id, null, target.Number, ImportChangeKind.New,
                        status, null));
                    continue;
                }

                var previous = existing.GetValueOrDefault((student.Id, target.Task.Id))?.Status;
                var kind = previous is null ? ImportChangeKind.New
                    : previous == status ? ImportChangeKind.Unchanged
                    : ImportChangeKind.Changed;
                changes.Add(new ImportChangeDto(row.Row, student.Id, target.Task.Id, target.Number, kind, status,
                    previous));
            }
        }

        var missingStudents = group.Students.Where(s => !rowsByStudent.ContainsKey(s.Id)).ToDtos();
        return new ImportPlan(foreign, sameLesson ? null : file.Meta?.LessonId, mismatch, changes, unrecognized,
            missingStudents, missingNumbers.Order(TaskNumber.Comparer).ToList(), cellErrors, warnings, errors);
    }

    private static (Student? Student, string? Reason, IReadOnlyList<Student> Candidates) Resolve(ParsedRow row,
        Group group)
    {
        if (row.StudentId is { } studentId && group.Students.FirstOrDefault(s => s.Id == studentId) is { } byId)
            return (byId, null, []);

        var tokens = Names.Tokens(row.Name);
        if (tokens.Count == 0)
            return (null, "Нет ФИО и id ученика.", []);

        var exact = group.Students.Where(s => Matches(tokens, s)).ToList();
        if (exact.Count == 1)
            return (exact[0], null, []);
        if (exact.Count > 1)
            return (null, "Несколько учеников с таким ФИО.", exact);

        var similar = group.Students
            .Where(s => tokens.Contains(Names.Normalize(s.LastName)))
            .ToList();
        return (null, "Ученик не найден в группе.", similar);
    }

    /// <summary>
    /// Совпадение без учёта порядка слов («Имя Фамилия» тоже): фамилия и имя обязательны,
    /// третье слово, если есть, — отчество.
    /// </summary>
    private static bool Matches(IReadOnlyList<string> tokens, Student student)
    {
        var last = Names.Normalize(student.LastName);
        var first = Names.Normalize(student.FirstName);
        var middle = Names.Normalize(student.MiddleName);

        var rest = tokens.ToList();
        if (!rest.Remove(last) || !rest.Remove(first))
            return false;
        return rest.Count == 0 || (rest.Count == 1 && rest[0] == middle);
    }
}
