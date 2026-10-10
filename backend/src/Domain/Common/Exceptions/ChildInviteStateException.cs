namespace SchoolClubs.Domain.Common.Exceptions;

/// <summary>Приглашение уже использовано, отозвано или истекло.</summary>
public class ChildInviteStateException() : InvalidOperationException("invite is used, revoked or expired");
