using SchoolClubs.Application.Common.Interfaces;
using IdentityHasher = Microsoft.AspNetCore.Identity.PasswordHasher<SchoolClubs.Domain.Entities.Users.UserLogin>;
using IdentityResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace SchoolClubs.Infrastructure.Identity;

/// <summary>PBKDF2 из ASP.NET Core Identity — без остальной Identity с её моделью пользователя.</summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly IdentityHasher _hasher = new();

    public string Hash(string password)
    {
        return _hasher.HashPassword(null!, password);
    }

    public PasswordVerification Verify(string hash, string password)
    {
        return _hasher.VerifyHashedPassword(null!, hash, password) switch
        {
            IdentityResult.Success => PasswordVerification.Success,
            IdentityResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed,
        };
    }
}
