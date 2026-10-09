namespace SchoolClubs.Domain.Common.Exceptions;

/// <summary>В занятии уже есть задача с таким номером.</summary>
public class DuplicateTaskNumberException(string number)
    : ArgumentException($"task number {number} is already used in the lesson", "number")
{
    public string Number { get; } = number;
}
