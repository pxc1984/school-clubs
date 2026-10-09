using System.Text.Json.Serialization;
using SchoolClubs.Application.Services.Common;
using SchoolClubs.Domain.Entities.Gradebook;

namespace SchoolClubs.Application.Services.GradebookImport;

[JsonConverter(typeof(JsonStringEnumConverter<ImportChangeKind>))]
public enum ImportChangeKind
{
    New,
    Changed,
    Unchanged,
}

/// <param name="TaskId"><c>null</c> — задачи с таким номером ещё нет (её создаст применение с CreateMissingTasks).</param>
public record ImportChangeDto(
    int Row,
    Guid StudentId,
    Guid? TaskId,
    long TaskNumber,
    ImportChangeKind Kind,
    GradebookEntryTaskStatus Status,
    GradebookEntryTaskStatus? PreviousStatus);

/// <param name="Candidates">Ученики группы, похожие по ФИО, — для ручного выбора.</param>
public record UnrecognizedRowDto(int Row, string Name, string Reason, IReadOnlyList<StudentDto> Candidates);

/// <param name="UploadId">Идентификатор загрузки для <c>…/import/{uploadId}/apply</c>.</param>
/// <param name="ForeignFile">Нет листа <c>_meta</c>: сопоставление только по тексту.</param>
/// <param name="LessonMismatch">Файл от другого занятия («файл от занятия 12.09»); применение — только с подтверждением.</param>
/// <param name="MissingTaskNumbers">Номера задач из файла, которых нет в занятии («создать задачи 4–5»).</param>
/// <param name="MissingStudents">Ученики группы, которых нет в файле: их отметки не трогаются.</param>
/// <param name="CellErrors">Нераспознанные отметки: <c>D7: «решил почти»</c>.</param>
/// <param name="Errors">Блокирующие ошибки (дубль ученика) — применение невозможно.</param>
public record ImportPreviewDto(
    Guid UploadId,
    string FileName,
    bool ForeignFile,
    string? LessonMismatch,
    IReadOnlyList<ImportChangeDto> Changes,
    IReadOnlyList<UnrecognizedRowDto> UnrecognizedRows,
    IReadOnlyList<StudentDto> MissingStudents,
    IReadOnlyList<long> MissingTaskNumbers,
    IReadOnlyList<string> CellErrors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

/// <param name="StudentId"><c>null</c> — пропустить строку.</param>
public record RowMappingRequest(int Row, Guid? StudentId);

public record ApplyImportRequest(
    bool ConfirmLessonMismatch,
    bool CreateMissingTasks,
    bool Publish,
    IReadOnlyList<RowMappingRequest>? RowMappings);

public record ImportResultDto(int Created, int Updated, int Unchanged, bool Published);

/// <param name="Attached">Этот файл сейчас прикреплён к журналу (последний применённый импорт).</param>
public record ImportFileDto(Guid UploadId, string FileName, long SizeBytes, DateTime UploadedAt, bool Attached);

public record FileDto(string FileName, string ContentType, byte[] Content);
