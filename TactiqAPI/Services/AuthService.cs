using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TactiqAPI.Models;

namespace TactiqAPI.Services;

public interface IAuthService
{
    Task<(bool success, string message, User? user)> RegisterAsync(string username, string email, string password);
    Task<(bool success, string message, User? user)> LoginAsync(string email, string password);
    string GenerateJwtToken(User user);
}

public class AuthService : IAuthService
{
    private readonly TactiqAPI.Data.TactiqDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(TactiqAPI.Data.TactiqDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool success, string message, User? user)> RegisterAsync(string username, string email, string password)
    {
        // Validations
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Tüm alanlar gereklidir.", null);

        if (password.Length < 6)
            return (false, "Şifre en az 6 karakter olmalıdır.", null);

        // Check if user exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email || u.Username == username);
        if (existingUser != null)
            return (false, "Bu email veya kullanıcı adı zaten kayıtlıdır.", null);

        // Create new user
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, "Kayıt başarılı!", user);
    }

    public async Task<(bool success, string message, User? user)> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "Email ve şifre gereklidir.", null);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Email veya şifre yanlış.", null);

        return (true, "Giriş başarılı!", user);
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            throw new InvalidOperationException("JWT configuration is missing.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
