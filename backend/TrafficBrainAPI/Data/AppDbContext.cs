using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Models;

namespace TrafficBrainAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Authentication & Users
        public DbSet<User> Users { get; set; }
        public DbSet<JunctionOverride> JunctionOverrides { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        // Location
        public DbSet<Region> Regions { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Junction> Junctions { get; set; }
        public DbSet<Lane> Lanes { get; set; }

        // Traffic Data
        public DbSet<VehicleCount> VehicleCounts { get; set; }
        public DbSet<SignalPhase> SignalPhases { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<EmergencyEvent> EmergencyEvents { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<ViolationEvent> ViolationEvents { get; set; }

        // Analytics
        public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
        public DbSet<TrafficPrediction> TrafficPredictions { get; set; }
        public DbSet<WeatherCondition> WeatherConditions { get; set; }
        public DbSet<ShiftRecord> ShiftRecords { get; set; }
        public DbSet<SmsAlert> SmsAlerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── User ──
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Email).IsRequired().HasMaxLength(200);
                e.Property(x => x.FullName).IsRequired().HasMaxLength(200);
                e.HasIndex(x => x.Email).IsUnique();
            });

            // ── Region ──
            modelBuilder.Entity<Region>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            });

            // ── District ──
            modelBuilder.Entity<District>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Region)
                 .WithMany(r => r.Districts)
                 .HasForeignKey(x => x.RegionId);
            });

            // ── City ──
            modelBuilder.Entity<City>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.District)
                 .WithMany(d => d.Cities)
                 .HasForeignKey(x => x.DistrictId);
            });

            // ── Junction ──
            modelBuilder.Entity<Junction>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.District)
                 .WithMany(d => d.Junctions)
                 .HasForeignKey(x => x.DistrictId);
                e.HasOne(x => x.City)
                 .WithMany(c => c.Junctions)
                 .HasForeignKey(x => x.CityId);
            });

            // ── Lane ──
            modelBuilder.Entity<Lane>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.Lanes)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── VehicleCount ──
            modelBuilder.Entity<VehicleCount>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.VehicleCounts)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── SignalPhase ──
            modelBuilder.Entity<SignalPhase>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.SignalPhases)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Incident ──
            modelBuilder.Entity<Incident>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.Incidents)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.ReportedByUser)
                 .WithMany()
                 .HasForeignKey(x => x.ReportedById)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── EmergencyEvent ──
            modelBuilder.Entity<EmergencyEvent>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.EmergencyEvents)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Alert ──
            modelBuilder.Entity<Alert>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.Alerts)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── ViolationEvent ──
            modelBuilder.Entity<ViolationEvent>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany()
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── JunctionOverride ──
            modelBuilder.Entity<JunctionOverride>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.Overrides)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Officer)
                 .WithMany(u => u.Overrides)
                 .HasForeignKey(x => x.OfficerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── PerformanceMetric ──
            modelBuilder.Entity<PerformanceMetric>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.PerformanceMetrics)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── TrafficPrediction ──
            modelBuilder.Entity<TrafficPrediction>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Junction)
                 .WithMany(j => j.Predictions)
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── SystemLog ──
            modelBuilder.Entity<SystemLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.User)
                 .WithMany(u => u.Logs)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── ShiftRecord ──
            modelBuilder.Entity<ShiftRecord>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Officer)
                 .WithMany()
                 .HasForeignKey(x => x.OfficerId)
                 .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Junction)
                 .WithMany()
                 .HasForeignKey(x => x.JunctionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Seed Data ──
            // Regions
            modelBuilder.Entity<Region>().HasData(
                new Region { Id = 1, Name = "Northern" },
                new Region { Id = 2, Name = "Central" },
                new Region { Id = 3, Name = "Southern" }
            );

            // Districts
            modelBuilder.Entity<District>().HasData(
                new District { Id = 1, RegionId = 2, Name = "Lilongwe" },
                new District { Id = 2, RegionId = 3, Name = "Blantyre" },
                new District { Id = 3, RegionId = 1, Name = "Mzimba" },
                new District { Id = 4, RegionId = 3, Name = "Zomba" }
            );

            // Cities
            modelBuilder.Entity<City>().HasData(
                new City { Id = 1, DistrictId = 1, Name = "Lilongwe" },
                new City { Id = 2, DistrictId = 2, Name = "Blantyre" },
                new City { Id = 3, DistrictId = 3, Name = "Mzuzu" },
                new City { Id = 4, DistrictId = 4, Name = "Zomba" }
            );

            // Junctions
            modelBuilder.Entity<Junction>().HasData(
                new Junction { Id = 1, DistrictId = 1, CityId = 1, Name = "Kamuzu Procession Rd Junction", RoadNames = "Kamuzu Procession Rd / Kenyatta Rd", Latitude = -13.9626, Longitude = 33.7741 },
                new Junction { Id = 2, DistrictId = 1, CityId = 1, Name = "Glyn Jones Rd Junction", RoadNames = "Glyn Jones Rd / Paul Kagame Rd", Latitude = -13.9689, Longitude = 33.7878 },
                new Junction { Id = 3, DistrictId = 1, CityId = 1, Name = "Area 18 Junction", RoadNames = "Presidential Way / Area 18 Rd", Latitude = -13.9745, Longitude = 33.7812 },
                new Junction { Id = 4, DistrictId = 2, CityId = 2, Name = "Chipembere Highway Junction", RoadNames = "Chipembere Highway / Victoria Ave", Latitude = -15.7861, Longitude = 35.0058 },
                new Junction { Id = 5, DistrictId = 2, CityId = 2, Name = "Kamuzu Highway Junction", RoadNames = "Kamuzu Highway / Kidney Crescent", Latitude = -15.7925, Longitude = 35.0021 },
                new Junction { Id = 6, DistrictId = 3, CityId = 3, Name = "Mzuzu City Centre Junction", RoadNames = "M1 Road / Orton Chirwa Ave", Latitude = -11.4657, Longitude = 34.0199 },
                new Junction { Id = 7, DistrictId = 4, CityId = 4, Name = "Zomba Main Junction", RoadNames = "M3 Road / College Rd", Latitude = -15.3833, Longitude = 35.3167 }
            );

            // Admin user seed
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "System Administrator",
                    Email = "admin@trafficbrain.mw",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2026"),
                    Role = "Admin",
                    District = "Lilongwe",
                    BadgeNumber = "TB-ADMIN-001",
                    PhoneNumber = "+265999000001",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}