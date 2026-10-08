namespace SchoolClubs.Application.Common.Exceptions;

/// <summary>Состояние не позволяет выполнить запрос (дубль email, повторная обработка) — 409.</summary>
public class ConflictException(string message) : Exception(message);
