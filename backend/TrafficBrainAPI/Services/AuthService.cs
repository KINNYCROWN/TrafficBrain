using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.Models;

namespace TrafficBrainAPI.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, User? User, string? AccessToken, string? RefreshToken)> LoginAsync(string email, string password, string ipAddress);
        Task<(bool Success, string Message, string? AccessToken, string? RefreshToken)> RefreshTokenAsync(string refreshToken);
        Task<bool> LogoutAsync(int userId);
        Task<(bool Success, string Message)> RegisterAsync(User user, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<(bool Success, string Message, User? User, string? AccessToken, string? RefreshToken)> LoginAsync(string email, string password, string ipAddress)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
                return (false, "Invalid email or password", null, null, null);

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return (false, "Invalid email or password", null, null, null);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            user.LastLogin = DateTime.UtcNow;

            // Log the login
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = user.Id,
                Action = "LOGIN",
                Details = $"User {user.Email} logged in",
                IpAddress = ipAddress,
                UserRole = user.Role,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return (true, "Login successful", user, accessToken, refreshToken);
        }

        public async Task<(bool Success, string Message, string? AccessToken, string? RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken
                    && u.RefreshTokenExpiry > DateTime.UtcNow
                    && u.IsActive);

            if (user == null)
                return (false, "Invalid or expired refresh token", null, null);

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return (true, "Token refreshed", newAccessToken, newRefreshToken);
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;

            _context.SystemLogs.Add(new SystemLog
            {
                UserId = user.Id,
                Action = "LOGOUT",
                Details = $"User {user.Email} logged out",
                UserRole = user.Role,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(User user, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return (false, "Email already exists");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return (true, "User registered successfully");
        }
    }
}