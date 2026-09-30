using SecureNotes.Core.Entities;

namespace SecureNotes.Core.Interfaces;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> LoginAsync(string email, string password);
    Task<User> RegisterAsync(string username, string email, string password);
    Task LogoutAsync(string token);
    Task<bool> IsTokenBlacklistedAsync(string token);
}