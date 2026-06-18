using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.Models;
using TrafficBrainAPI.Services;
using System.Text;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IAnalyticsService _analytics;

        public AnalyticsController(AppDbContext context, IAnalyticsService analytics)
        {
            _context = context;
            _analytics = analytics;
        }

        // GET: api/analytics/national
        [HttpGet("national")]
        public async Task<ActionResult> GetNationalStats()
        {
            var stats = await _analytics.GetNationalStatsAsync();
            return Ok(stats);
        }

        // GET: api/analytics/performance/5
        [HttpGet("performance/{junctionId}")]
        public async Task<ActionResult> GetJunctionPerformance(int junctionId)
        {
            var performance = await _analytics.GetJunctionPerformanceAsync(junctionId);
            return Ok(performance);
        }

        // GET: api/analytics/peakhours/5
        [HttpGet("peakhours/{junctionId}")]
        public async Task<ActionResult> GetPeakHours(int junctionId)
        {
            var peakHours = await _analytics.GetPeakHoursAsync(junctionId);
            return Ok(peakHours);
        }

        // GET: api/analytics/district/Lilongwe
        [HttpGet("district/{district}")]
        public async Task<ActionResult> GetDistrictSummary(string district)
        {
            var summary = await _analytics.GetDistrictSummaryAsync(district);
            return Ok(summary);
        }

        // GET: api/analytics/comparison/5
        [HttpGet("comparison/{junctionId}")]
        public async Task<ActionResult> GetAIvsFixedComparison(int junctionId)
        {
            var comparison = await _analytics.GetAIvsFixedComparisonAsync(junctionId);
            return Ok(comparison);
        }

        // GET: api/analytics/environment
        [HttpGet("environment")]
        public async Task<ActionResult> GetEnvironmentalImpact()
        {
            var impact = await _analytics.GetEnvironmentalImpactAsync();
            return Ok(impact);
        }

        // POST: api/analytics/performance
        [HttpPost("performance")]
        public async Task<ActionResult> RecordPerformanceMetric(PerformanceMetric metric)
        {
            var junction = await _context.Junctions.FindAsync(metric.JunctionId);
            if (junction == null)
                return BadRequest(new { error = "Junction not found" });

            metric.RecordedAt = DateTime.UtcNow;
            _context.PerformanceMetrics.Add(metric);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Performance metric recorded" });
        }

        // GET: api/analytics/export/5
        [HttpGet("export/{junctionId}")]
        [Authorize]
        public async Task<ActionResult> ExportJunctionData(
            int junctionId,
            [FromQuery] int days = 7)
        {
            var junction = await _context.Junctions
                .Include(j => j.District)
                .FirstOrDefaultAsync(j => j.Id == junctionId);

            if (junction == null) return NotFound();

            var since = DateTime.UtcNow.AddDays(-days);
            var counts = await _context.VehicleCounts
                .Where(v => v.JunctionId == junctionId && v.RecordedAt >= since)
                .OrderBy(v => v.RecordedAt)
                .ToListAsync();

            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine("Timestamp,Cars,Motorcycles,Buses,Trucks,Pedestrians,Total,AvgSpeed,CongestionScore");

            foreach (var count in counts)
            {
                csv.AppendLine($"{count.RecordedAt:yyyy-MM-dd HH:mm:ss}," +
                    $"{count.Cars},{count.Motorcycles},{count.Buses}," +
                    $"{count.Trucks},{count.Pedestrians},{count.TotalVehicles}," +
                    $"{count.AverageSpeed},{count.CongestionScore}");
            }

            var fileName = $"TrafficBrain_{junction.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv";
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            return File(bytes, "text/csv", fileName);
        }

        // GET: api/analytics/predictions/5
        [HttpGet("predictions/{junctionId}")]
        public async Task<ActionResult> GetPredictions(int junctionId)
        {
            // Simple prediction based on historical averages
            var last7days = DateTime.UtcNow.AddDays(-7);

            var hourlyAverages = await _context.VehicleCounts
                .Where(v => v.JunctionId == junctionId && v.RecordedAt >= last7days)
                .GroupBy(v => v.RecordedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    AvgVehicles = g.Average(v => v.TotalVehicles),
                    AvgCongestion = g.Average(v => v.CongestionScore)
                })
                .OrderBy(h => h.Hour)
                .ToListAsync();

            var predictions = hourlyAverages.Select(h => new
            {
                PredictedFor = DateTime.UtcNow.Date.AddHours(h.Hour),
                h.Hour,
                ExpectedVehicles = (int)h.AvgVehicles,
                ExpectedCongestion = (int)h.AvgCongestion,
                Confidence = 0.75,
                Label = h.AvgCongestion >= 75 ? "Critical" :
                        h.AvgCongestion >= 50 ? "Heavy" :
                        h.AvgCongestion >= 25 ? "Moderate" : "Light"
            });

            return Ok(new
            {
                JunctionId = junctionId,
                GeneratedAt = DateTime.UtcNow,
                Predictions = predictions
            });
        }

        // GET: api/analytics/vehicles/breakdown
        [HttpGet("vehicles/breakdown")]
        public async Task<ActionResult> GetVehicleBreakdown([FromQuery] int hours = 24)
        {
            var since = DateTime.UtcNow.AddHours(-hours);

            var breakdown = await _context.VehicleCounts
                .Where(v => v.RecordedAt >= since)
                .GroupBy(v => 1)
                .Select(g => new
                {
                    TotalCars = g.Sum(v => v.Cars),
                    TotalMotorcycles = g.Sum(v => v.Motorcycles),
                    TotalBuses = g.Sum(v => v.Buses),
                    TotalTrucks = g.Sum(v => v.Trucks),
                    TotalPedestrians = g.Sum(v => v.Pedestrians),
                    TotalVehicles = g.Sum(v => v.TotalVehicles),
                    AverageCongestion = g.Average(v => v.CongestionScore)
                })
                .FirstOrDefaultAsync();

            return Ok(breakdown ?? new
            {
                TotalCars = 0,
                TotalMotorcycles = 0,
                TotalBuses = 0,
                TotalTrucks = 0,
                TotalPedestrians = 0,
                TotalVehicles = 0,
                AverageCongestion = 0.0
            });
        }
    }
}