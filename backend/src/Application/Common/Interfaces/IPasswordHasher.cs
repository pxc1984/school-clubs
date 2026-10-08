namespace SchoolClubs.Application.Common.Interfaces;

public enum PasswordVerification
{
    Failed,
    Success,

    /// <summary>Пароль верный, но хеш устарел и его стоит пересчитать.</summary>
    SuccessRehashNeeded,
}

public interface IPasswordHasher
{
    string Hash(string password);
    PasswordVerification Verify(string hash, string password);
}
