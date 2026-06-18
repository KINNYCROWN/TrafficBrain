using System.ComponentModel.DataAnnotations;

namespace TrafficBrainAPI.DTOs
{
    // ─────────────────────────────────────────
    // AUTH DTOs
    // ─────────────────────────────────────────
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string BadgeNumber { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Viewer";

        public string District { get; set; } = string.Empty;
        public string BadgeNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    // ─────────────────────────────────────────
    // TRAFFIC DTOs
    // ─────────────────────────────────────────
    public class VehicleCountRequest
    {
        [Required]
        public int JunctionId { get; set; }
        public int? LaneId { get; set; }
        public int Cars { get; set; }
        public int Motorcycles { get; set; }
        public int Buses { get; set; }
        public int Trucks { get; set; }
        public int Pedestrians { get; set; }
        public int TotalVehicles { get; set; }
        public double AverageSpeed { get; set; }
        public int CongestionScore { get; set; }
        public string Lane { get; set; } = "Main";
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    public class SignalOverrideRequest
    {
        [Required]
        public int JunctionId { get; set; }

        [Required]
        public string Mode { get; set; } = string.Empty; // Auto, Manual, Emergency

        public string ForcedDirection { get; set; } = string.Empty;
        public int? CustomGreenDuration { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class IncidentRequest
    {
        [Required]
        public int JunctionId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Severity { get; set; } = string.Empty;
    }

    public class EmergencyRequest
    {
        [Required]
        public int JunctionId { get; set; }

        [Required]
        public string VehicleType { get; set; } = string.Empty;

        [Required]
        public string Direction { get; set; } = string.Empty;
    }

    public class VIPCorridorRequest
    {
        [Required]
        public List<int> JunctionIds { get; set; } = new();

        [Required]
        public string Reason { get; set; } = string.Empty;

        public int HoldDurationSeconds { get; set; } = 120;
    }

    public class SmsAlertRequest
    {
        [Required]
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public int? JunctionId { get; set; }
    }
}