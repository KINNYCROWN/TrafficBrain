namespace TrafficBrainAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Viewer"; // Admin, Supervisor, TrafficOfficer, Viewer
        public string District { get; set; } = string.Empty;
        public string BadgeNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<JunctionOverride> Overrides { get; set; } = new List<JunctionOverride>();
        public ICollection<SystemLog> Logs { get; set; } = new List<SystemLog>();
    }

    public class JunctionOverride
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public int OfficerId { get; set; }
        public string Mode { get; set; } = string.Empty; // Auto, Manual, Emergency
        public string Reason { get; set; } = string.Empty;
        public string ForcedDirection { get; set; } = string.Empty; // North, South, East, West, All
        public int? CustomGreenDuration { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }

        // Navigation
        public Junction? Junction { get; set; }
        public User? Officer { get; set; }
    }

    public class SystemLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int? JunctionId { get; set; }
        public string Details { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }
}