using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.DTOs;
using TrafficBrainAPI.Models;
using TrafficBrainAPI.Services;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(IAuthService authService, AppDbContext context, IConfiguration config)
        {
            _authService = authService;
            _context = context;
            _config = config;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var (success, message, user, accessToken, refreshToken) =
                await _authService.LoginAsync(request.Email, request.Password, ipAddress);

            if (!success)
                return Unauthorized(new { message });

            return Ok(new LoginResponse
            {
                AccessToken = accessToken!,
                RefreshToken = refreshToken!,
                FullName = user!.FullName,
                Email = user.Email,
                Role = user.Role,
                District = user.District,
                BadgeNumber = user.BadgeNumber,
                ExpiresAt = DateTime.UtcNow.AddMinutes(
                    int.Parse(_config["Jwt:ExpiryMinutes"]!))
            });
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Register(RegisterRequest request)
        {
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Role = request.Role,
                District = request.District,
                BadgeNumber = request.BadgeNumber,
                PhoneNumber = request.PhoneNumber
            };

            var (success, message) = await _authService.RegisterAsync(user, request.Password);

            if (!success)
                return BadRequest(new { message });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = currentUserId,
                Action = "REGISTER_USER",
                Details = $"Registered new user {request.Email} with role {request.Role}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new { message });
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<ActionResult> RefreshToken(RefreshTokenRequest request)
        {
            var (success, message, accessToken, refreshToken) =
                await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!success)
                return Unauthorized(new { message });

            return Ok(new { accessToken, refreshToken, message });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.LogoutAsync(userId);
            return Ok(new { message = "Logged out successfully" });
        }

        // GET: api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.District,
                user.BadgeNumber,
                user.PhoneNumber,
                user.LastLogin,
                user.CreatedAt
            });
        }

        // PUT: api/auth/changepassword
        [HttpPut("changepassword")]
        [Authorize]
        public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // GET: api/auth/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.District,
                    u.BadgeNumber,
                    u.PhoneNumber,
                    u.IsActive,
                    u.LastLogin,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // PUT: api/auth/users/5/deactivate
        [HttpPut("users/{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false;
            user.RefreshToken = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {user.Email} deactivated" });
        }
    }
}