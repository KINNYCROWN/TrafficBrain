using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.Models;

namespace TrafficBrainAPI.Services
{
    public interface IAnalyticsService
    {
        Task<object> GetNationalStatsAsync();
        Task<object> GetJunctionPerformanceAsync(int junctionId);
        Task<object> GetPeakHoursAsync(int junctionId);
        Task<object> GetDistrictSummaryAsync(string district);
        Task<object> GetAIvsFixedComparisonAsync(int junctionId);
        Task<object> GetEnvironmentalImpactAsync();
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly AppDbContext _context;

        public AnalyticsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetNationalStatsAsync()
        {
            var totalJunctions = await _context.Junctions.CountAsync(j => j.IsActive);
            var onlineJunctions = await _context.Junctions.CountAsync(j => j.IsOnline);
            var totalVehiclesToday = await _context.VehicleCounts
                .Where(v => v.RecordedAt >= DateTime.UtcNow.Date)
                .SumAsync(v => v.TotalVehicles);
            var activeIncidents = await _context.Incidents
                .CountAsync(i => !i.IsResolved);
            var activeEmergencies = await _context.EmergencyEvents
                .CountAsync(e => !e.CorridorCleared);
            var totalDistricts = await _context.Districts.CountAsync(d => d.IsActive);
            var unacknowledgedAlerts = await _context.Alerts
                .CountAsync(a => !a.IsAcknowledged);
            var totalCO2Saved = await _context.PerformanceMetrics
                .SumAsync(p => p.CO2SavedKg);

            return new
            {
                TotalJunctions = totalJunctions,
                OnlineJunctions = onlineJunctions,
                OfflineJunctions = totalJunctions - onlineJunctions,
                TotalVehiclesToday = totalVehiclesToday,
                ActiveIncidents = activeIncidents,
                ActiveEmergencies = activeEmergencies,
                TotalDistricts = totalDistricts,
                UnacknowledgedAlerts = unacknowledgedAlerts,
                TotalCO2SavedKg = Math.Round(totalCO2Saved, 2),
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<object> GetJunctionPerformanceAsync(int junctionId)
        {
            var junction = await _context.Junctions
                .Include(j => j.District)
                .Include(j => j.City)
                .FirstOrDefaultAsync(j => j.Id == junctionId);

            if (junction == null) return new { Error = "Junction not found" };

            var last24h = DateTime.UtcNow.AddHours(-24);

            var vehicleCounts = await _context.VehicleCounts
                .Where(v => v.JunctionId == junctionId && v.RecordedAt >= last24h)
                .OrderBy(v => v.RecordedAt)
                .ToListAsync();

            var avgCongestion = vehicleCounts.Any()
                ? vehicleCounts.Average(v => v.CongestionScore) : 0;
            var peakVehicles = vehicleCounts.Any()
                ? vehicleCounts.Max(v => v.TotalVehicles) : 0;
            var totalVehicles = vehicleCounts.Sum(v => v.TotalVehicles);

            var recentCounts = vehicleCounts
                .TakeLast(20)
                .Select(v => new
                {
                    v.RecordedAt,
                    v.TotalVehicles,
                    v.CongestionScore,
                    v.AverageSpeed,
                    v.Cars,
                    v.Motorcycles,
                    v.Buses,
                    v.Trucks,
                    v.Pedestrians
                });

            return new
            {
                Junction = new
                {
                    junction.Id,
                    junction.Name,
                    District = junction.District?.Name,
                    City = junction.City?.Name,
                    junction.ControlMode,
                    junction.IsOnline,
                    junction.LastDataReceived
                },
                Stats = new
                {
                    TotalVehiclesLast24h = totalVehicles,
                    AverageCongestionScore = Math.Round(avgCongestion, 1),
                    PeakVehicleCount = peakVehicles,
                    DataPoints = vehicleCounts.Count
                },
                RecentCounts = recentCounts
            };
        }

        public async Task<object> GetPeakHoursAsync(int junctionId)
        {
            var last7days = DateTime.UtcNow.AddDays(-7);

            var hourlyData = await _context.VehicleCounts
                .Where(v => v.JunctionId == junctionId && v.RecordedAt >= last7days)
                .GroupBy(v => v.RecordedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    AverageVehicles = g.Average(v => v.TotalVehicles),
                    AverageCongestion = g.Average(v => v.CongestionScore),
                    TotalRecords = g.Count()
                })
                .OrderBy(h => h.Hour)
                .ToListAsync();

            return new
            {
                JunctionId = junctionId,
                HourlyBreakdown = hourlyData,
                PeakHour = hourlyData.Any()
                    ? hourlyData.OrderByDescending(h => h.AverageVehicles).First().Hour
                    : 0
            };
        }

        public async Task<object> GetDistrictSummaryAsync(string district)
        {
            var junctions = await _context.Junctions
                .Include(j => j.District)
                .Where(j => j.District!.Name.ToLower() == district.ToLower() && j.IsActive)
                .ToListAsync();

            var junctionIds = junctions.Select(j => j.Id).ToList();
            var today = DateTime.UtcNow.Date;

            var totalVehiclesToday = await _context.VehicleCounts
                .Where(v => junctionIds.Contains(v.JunctionId) && v.RecordedAt >= today)
                .SumAsync(v => v.TotalVehicles);

            var activeIncidents = await _context.Incidents
                .Where(i => junctionIds.Contains(i.JunctionId) && !i.IsResolved)
                .CountAsync();

            var avgCongestion = await _context.VehicleCounts
                .Where(v => junctionIds.Contains(v.JunctionId)
                    && v.RecordedAt >= DateTime.UtcNow.AddHours(-1))
                .AverageAsync(v => (double?)v.CongestionScore) ?? 0;

            return new
            {
                District = district,
                TotalJunctions = junctions.Count,
                OnlineJunctions = junctions.Count(j => j.IsOnline),
                TotalVehiclesToday = totalVehiclesToday,
                ActiveIncidents = activeIncidents,
                AverageCongestionScore = Math.Round(avgCongestion, 1),
                Junctions = junctions.Select(j => new
                {
                    j.Id,
                    j.Name,
                    j.ControlMode,
                    j.IsOnline,
                    j.Latitude,
                    j.Longitude
                })
            };
        }

        public async Task<object> GetAIvsFixedComparisonAsync(int junctionId)
        {
            var metrics = await _context.PerformanceMetrics
                .Where(p => p.JunctionId == junctionId)
                .OrderByDescending(p => p.RecordedAt)
                .Take(100)
                .ToListAsync();

            var adaptive = metrics.Where(m => m.ControlMode == "Adaptive").ToList();
            var fixed_timer = metrics.Where(m => m.ControlMode == "Fixed").ToList();

            var adaptiveAvgWait = adaptive.Any() ? adaptive.Average(m => m.AverageWaitTime) : 0;
            var fixedAvgWait = fixed_timer.Any() ? fixed_timer.Average(m => m.AverageWaitTime) : 0;

            var improvement = fixedAvgWait > 0
                ? Math.Round((fixedAvgWait - adaptiveAvgWait) / fixedAvgWait * 100, 1)
                : 0;

            return new
            {
                JunctionId = junctionId,
                Adaptive = new
                {
                    AverageWaitTime = Math.Round(adaptiveAvgWait, 2),
                    TotalVehiclesProcessed = adaptive.Sum(m => m.TotalVehiclesProcessed),
                    AverageCongestionScore = adaptive.Any() ? Math.Round(adaptive.Average(m => m.CongestionScore), 1) : 0,
                    CO2SavedKg = Math.Round(adaptive.Sum(m => m.CO2SavedKg), 2)
                },
                Fixed = new
                {
                    AverageWaitTime = Math.Round(fixedAvgWait, 2),
                    TotalVehiclesProcessed = fixed_timer.Sum(m => m.TotalVehiclesProcessed),
                    AverageCongestionScore = fixed_timer.Any() ? Math.Round(fixed_timer.Average(m => m.CongestionScore), 1) : 0,
                    CO2SavedKg = 0
                },
                ImprovementPercent = improvement,
                Conclusion = improvement > 0
                    ? $"TRAFFICBRAIN reduces average wait time by {improvement}% compared to fixed-timer control"
                    : "Insufficient data for comparison"
            };
        }

        public async Task<object> GetEnvironmentalImpactAsync()
        {
            var totalCO2Saved = await _context.PerformanceMetrics
                .SumAsync(p => p.CO2SavedKg);
            var totalFuelSaved = await _context.PerformanceMetrics
                .SumAsync(p => p.FuelWasteEstimateLitres);
            var totalVehicles = await _context.VehicleCounts
                .SumAsync(v => v.TotalVehicles);

            return new
            {
                TotalCO2SavedKg = Math.Round(totalCO2Saved, 2),
                TotalCO2SavedTonnes = Math.Round(totalCO2Saved / 1000, 3),
                TotalFuelSavedLitres = Math.Round(totalFuelSaved, 2),
                TotalVehiclesManaged = totalVehicles,
                EquivalentTreesPlanted = Math.Round(totalCO2Saved / 21.7, 0),
                LastUpdated = DateTime.UtcNow
            };
        }
    }
}