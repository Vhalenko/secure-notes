namespace SecureNotes.Core.Interfaces;

public interface ITokenBlacklistService
{
    Task AddAsync(string token, TimeSpan expiry);
    Task<bool> IsBlacklistedAsync(string token);
}