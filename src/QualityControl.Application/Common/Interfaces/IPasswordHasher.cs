namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Password hashing. Kept behind an interface so the algorithm can be upgraded
/// without touching authentication logic.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash. Implementations must compare in
    /// constant time and must not throw on a malformed stored hash — return false.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
