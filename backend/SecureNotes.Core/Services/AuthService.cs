using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecureNotes.Core.Entities;
using SecureNotes.Core.Interfaces;
namespace SecureNotes.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly string _jwtSecret;
    private readonly int _jwtExpiryMinutes;

    public AuthService(
        IUserRepository userRepository,
        IAuditLogRepository auditLogRepository,
        ITokenBlacklistService blacklistService,
        string jwtSecret,
        int jwtExpiryMinutes = 60)
    {
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
        _blacklistService = blacklistService;
        _jwtSecret = jwtSecret;
        _jwtExpiryMinutes = jwtExpiryMinutes;
    }

    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                Action = "LOGIN",
                EntityType = "User",
                Success = false,
                Details = $"Failed login attempt for {email}",
            });

            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = Guid.NewGuid().ToString("N");

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = "LOGIN",
            EntityType = "User",
            EntityId = user.Id,
            UserId = user.Id,
            Success = true,
        });

        return (accessToken, refreshToken);
    }

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        var existingEmail = await _userRepository.GetByEmailAsync(email);
        if (existingEmail is not null)
            throw new InvalidOperationException("Email already in use.");

        var existingUsername = await _userRepository.GetByUsernameAsync(username);
        if (existingUsername is not null)
            throw new InvalidOperationException("Username already in use.");

        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        var created = await _userRepository.CreateAsync(user);

        await _auditLogRepository.CreateAsync(new AuditLog
        {
            Action = "REGISTER",
            EntityType = "User",
            EntityId = created.Id,
            UserId = created.Id,
            Success = true,
        });

        return created;
    }

    public async Task LogoutAsync(string token)
    {
        var expiry = TimeSpan.FromMinutes(_jwtExpiryMinutes);
        await _blacklistService.AddAsync(token, expiry);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        return await _blacklistService.IsBlacklistedAsync(token);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}