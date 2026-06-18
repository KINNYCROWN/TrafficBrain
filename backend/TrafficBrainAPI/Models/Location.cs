namespace TrafficBrainAPI.Models
{
    public class Region
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Northern, Central, Southern
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<District> Districts { get; set; } = new List<District>();
    }

    public class District
    {
        public int Id { get; set; }
        public int RegionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation
        public Region? Region { get; set; }
        public ICollection<City> Cities { get; set; } = new List<City>();
        public ICollection<Junction> Junctions { get; set; } = new List<Junction>();
    }

    public class City
    {
        public int Id { get; set; }
        public int DistrictId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation
        public District? District { get; set; }
        public ICollection<Junction> Junctions { get; set; } = new List<Junction>();
    }

    public class Junction
    {
        public int Id { get; set; }
        public int DistrictId { get; set; }
        public int CityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RoadNames { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ControlMode { get; set; } = "Auto"; // Auto, Manual, Emergency
        public bool IsActive { get; set; } = true;
        public bool IsOnline { get; set; } = false;
        public DateTime? LastDataReceived { get; set; }
        public DateTime CreatedAt { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Navigation
        public District? District { get; set; }
        public City? City { get; set; }
        public ICollection<Lane> Lanes { get; set; } = new List<Lane>();
        public ICollection<VehicleCount> VehicleCounts { get; set; } = new List<VehicleCount>();
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
        public ICollection<SignalPhase> SignalPhases { get; set; } = new List<SignalPhase>();
        public ICollection<EmergencyEvent> EmergencyEvents { get; set; } = new List<EmergencyEvent>();
        public ICollection<JunctionOverride> Overrides { get; set; } = new List<JunctionOverride>();
        public ICollection<PerformanceMetric> PerformanceMetrics { get; set; } = new List<PerformanceMetric>();
        public ICollection<TrafficPrediction> Predictions { get; set; } = new List<TrafficPrediction>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }

    public class Lane
    {
        public int Id { get; set; }
        public int JunctionId { get; set; }
        public string Direction { get; set; } = string.Empty; // North, South, East, West
        public int LaneNumber { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Junction? Junction { get; set; }
    }
}