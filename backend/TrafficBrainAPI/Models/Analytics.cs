namespace TrafficBrainAPI.Models
{
    public class PerformanceMetric
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public string ControlMode { get; set; } = string.Empty; // Fixed, Adaptive
        public double AverageWaitTime { get; set; }
        public double MaxWaitTime { get; set; }
        public int TotalVehiclesProcessed { get; set; }
        public double ThroughputPerHour { get; set; }
        public double FuelWasteEstimateLitres { get; set; }
        public double CO2SavedKg { get; set; }
        public int CongestionScore { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Junction? Junction { get; set; }
    }

    public class TrafficPrediction
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public DateTime PredictedFor { get; set; }
        public int ExpectedVehicleCount { get; set; }
        public int CongestionScore { get; set; }
        public double Confidence { get; set; }
        public string PredictionBasis { get; set; } = string.Empty; // Historical, Weather, Event
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Junction? Junction { get; set; }
    }

    public class WeatherCondition
    {
        public int Id { get; set; }
        public int DistrictId { get; set; }
        public string Condition { get; set; } = string.Empty; // Clear, Rain, Fog, Storm
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double Visibility { get; set; }
        public string WeatherImpact { get; set; } = string.Empty; // None, Low, Medium, High
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    public class ShiftRecord
    {
        public int Id { get; set; }
        public int OfficerId { get; set; }
        public int JunctionId { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime? ShiftEnd { get; set; }
        public int TotalOverrides { get; set; }
        public int TotalIncidentsHandled { get; set; }
        public string HandoverNotes { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation
        public User? Officer { get; set; }
        public Junction? Junction { get; set; }
    }

    public class SmsAlert
    {
        public int Id { get; set; }
        public int? JunctionId { get; set; }
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
        public string? ProviderMessageId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SentAt { get; set; }
    }
}