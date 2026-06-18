namespace TrafficBrainAPI.Models
{
    public class VehicleCount
    {
        public int Id { get; set; }
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
        public string Lane { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Junction? Junction { get; set; }
        public Lane? LaneRef { get; set; }
    }

    public class SignalPhase
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public int PhaseNumber { get; set; }
        public string Direction { get; set; } = string.Empty;
        public int GreenDuration { get; set; }
        public int RedDuration { get; set; }
        public int YellowDuration { get; set; }
        public int QueueLengthAtChange { get; set; }
        public double WaitTimeBefore { get; set; }
        public double WaitTimeAfter { get; set; }
        public string TriggeredBy { get; set; } = "AI"; // AI, Officer, Emergency
        public string ControlMode { get; set; } = "Auto";
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Junction? Junction { get; set; }
    }

    public class Incident
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public int? ReportedById { get; set; }
        public string Type { get; set; } = string.Empty; // Accident, Breakdown, Congestion, Violation
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
        public bool IsResolved { get; set; } = false;
        public string? ResolvedBy { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Navigation
        public Junction? Junction { get; set; }
        public User? ReportedByUser { get; set; }
    }

    public class EmergencyEvent
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public string VehicleType { get; set; } = string.Empty; // Ambulance, FireTruck, Police
        public string Direction { get; set; } = string.Empty;
        public string CorridorJunctions { get; set; } = string.Empty; // JSON array of junction IDs
        public bool CorridorCleared { get; set; } = false;
        public double TimeSavedMinutes { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Navigation
        public Junction? Junction { get; set; }
    }

    public class Alert
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public string AlertType { get; set; } = string.Empty; // Congestion, Incident, Emergency, Offline, Violation
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
        public bool IsAcknowledged { get; set; } = false;
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Junction? Junction { get; set; }
    }

    public class ViolationEvent
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public string ViolationType { get; set; } = string.Empty; // RedLight, NoStopping, Speeding
        public string Direction { get; set; } = string.Empty;
        public double? Speed { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Junction? Junction { get; set; }
    }
}