using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.DTOs;
using TrafficBrainAPI.Models;
using TrafficBrainAPI.Hubs;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/traffic")]
    public class TrafficController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TrafficHub> _hub;

        public TrafficController(AppDbContext context, IHubContext<TrafficHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // POST: api/traffic/vehiclecount
        [HttpPost("vehiclecount")]
        public async Task<ActionResult> PostVehicleCount(VehicleCountRequest request)
        {
            // Verify junction exists
            var junction = await _context.Junctions.FindAsync(request.JunctionId);
            if (junction == null)
                return BadRequest(new { error = $"Junction {request.JunctionId} not found" });

            // Save to database
            var count = new VehicleCount
            {
                JunctionId = request.JunctionId,
                LaneId = request.LaneId,
                Cars = request.Cars,
                Motorcycles = request.Motorcycles,
                Buses = request.Buses,
                Trucks = request.Trucks,
                Pedestrians = request.Pedestrians,
                TotalVehicles = request.TotalVehicles,
                AverageSpeed = request.AverageSpeed,
                CongestionScore = request.CongestionScore,
                Lane = request.Lane,
                RecordedAt = DateTime.UtcNow
            };

            _context.VehicleCounts.Add(count);

            // Update junction online status
            junction.IsOnline = true;
            junction.LastDataReceived = DateTime.UtcNow;

            // Create congestion alert if score is high
            if (request.CongestionScore >= 75)
            {
                _context.Alerts.Add(new Alert
                {
                    JunctionId = request.JunctionId,
                    AlertType = "Congestion",
                    Message = $"Critical congestion at {junction.Name} — Score: {request.CongestionScore}/100",
                    Severity = "Critical",
                    CreatedAt = DateTime.UtcNow
                });

                await _hub.Clients.All.SendAsync("CongestionAlert", new
                {
                    JunctionId = request.JunctionId,
                    JunctionName = junction.Name,
                    CongestionScore = request.CongestionScore,
                    TotalVehicles = request.TotalVehicles,
                    Timestamp = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Broadcast to dashboard via SignalR
            await _hub.Clients.All.SendAsync("VehicleCountUpdated", new
            {
                request.JunctionId,
                JunctionName = junction.Name,
                request.Cars,
                request.Motorcycles,
                request.Buses,
                request.Trucks,
                request.Pedestrians,
                request.TotalVehicles,
                request.AverageSpeed,
                request.CongestionScore,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Vehicle count recorded and broadcasted" });
        }

        // GET: api/traffic/{junctionId}/live
        [HttpGet("{junctionId}/live")]
        public async Task<ActionResult> GetLiveData(int junctionId)
        {
            var latest = await _context.VehicleCounts
                .Where(v => v.JunctionId == junctionId)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            if (latest == null)
                return Ok(new { message = "No data yet" });

            return Ok(latest);
        }

        // GET: api/traffic/{junctionId}/counts
        [HttpGet("{junctionId}/counts")]
        public async Task<ActionResult> GetHistoricalCounts(
            int junctionId,
            [FromQuery] int hours = 1)
        {
            var since = DateTime.UtcNow.AddHours(-hours);
            var counts = await _context.VehicleCounts
                .Where(v => v.JunctionId == junctionId && v.RecordedAt >= since)
                .OrderByDescending(v => v.RecordedAt)
                .Take(200)
                .ToListAsync();

            return Ok(counts);
        }

        // POST: api/traffic/signal/override
        [HttpPost("signal/override")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> OverrideSignal(SignalOverrideRequest request)
        {
            var junction = await _context.Junctions.FindAsync(request.JunctionId);
            if (junction == null)
                return BadRequest(new { error = "Junction not found" });

            var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var officerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            var officerRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
            var officerDistrict = User.FindFirst("District")?.Value ?? "";

            // Check district permission for TrafficOfficer
            if (officerRole == "TrafficOfficer" &&
                !string.IsNullOrEmpty(officerDistrict))
            {
                var junctionDistrict = await _context.Districts
                    .FindAsync(junction.DistrictId);
                if (junctionDistrict?.Name != officerDistrict)
                    return Forbid();
            }

            // Deactivate existing overrides for this junction
            var existing = await _context.JunctionOverrides
                .Where(o => o.JunctionId == request.JunctionId && o.IsActive)
                .ToListAsync();
            existing.ForEach(o => { o.IsActive = false; o.EndTime = DateTime.UtcNow; });

            // Create new override
            var overrideRecord = new JunctionOverride
            {
                JunctionId = request.JunctionId,
                OfficerId = officerId,
                Mode = request.Mode,
                Reason = request.Reason,
                ForcedDirection = request.ForcedDirection,
                CustomGreenDuration = request.CustomGreenDuration,
                IsActive = true,
                StartTime = DateTime.UtcNow
            };

            _context.JunctionOverrides.Add(overrideRecord);

            // Update junction control mode
            junction.ControlMode = request.Mode;

            // Log the action
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = officerId,
                Action = "SIGNAL_OVERRIDE",
                JunctionId = request.JunctionId,
                Details = $"{officerName} set junction {junction.Name} to {request.Mode} mode. Reason: {request.Reason}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserRole = officerRole,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Broadcast override to dashboard
            await _hub.Clients.All.SendAsync("JunctionOverrideActivated", new
            {
                request.JunctionId,
                JunctionName = junction.Name,
                request.Mode,
                request.Reason,
                request.ForcedDirection,
                OfficerName = officerName,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { message = $"Junction {junction.Name} set to {request.Mode} mode" });
        }

        // POST: api/traffic/signal/auto
        [HttpPost("signal/auto")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> ReturnToAuto([FromBody] int junctionId)
        {
            var junction = await _context.Junctions.FindAsync(junctionId);
            if (junction == null) return BadRequest(new { error = "Junction not found" });

            var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var officerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            // Deactivate all overrides
            var overrides = await _context.JunctionOverrides
                .Where(o => o.JunctionId == junctionId && o.IsActive)
                .ToListAsync();
            overrides.ForEach(o => { o.IsActive = false; o.EndTime = DateTime.UtcNow; });

            junction.ControlMode = "Auto";

            _context.SystemLogs.Add(new SystemLog
            {
                UserId = officerId,
                Action = "RETURN_TO_AUTO",
                JunctionId = junctionId,
                Details = $"{officerName} returned junction {junction.Name} to AUTO mode",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("JunctionOverrideDeactivated", new
            {
                JunctionId = junctionId,
                JunctionName = junction.Name,
                OfficerName = officerName,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { message = $"Junction {junction.Name} returned to AUTO mode" });
        }

        // GET: api/traffic/{junctionId}/overrides
        [HttpGet("{junctionId}/overrides")]
        [Authorize]
        public async Task<ActionResult> GetOverrideHistory(int junctionId)
        {
            var overrides = await _context.JunctionOverrides
                .Include(o => o.Officer)
                .Where(o => o.JunctionId == junctionId)
                .OrderByDescending(o => o.StartTime)
                .Take(50)
                .Select(o => new
                {
                    o.Id,
                    o.Mode,
                    o.Reason,
                    o.ForcedDirection,
                    o.CustomGreenDuration,
                    o.IsActive,
                    o.StartTime,
                    o.EndTime,
                    OfficerName = o.Officer != null ? o.Officer.FullName : "Unknown",
                    OfficerBadge = o.Officer != null ? o.Officer.BadgeNumber : ""
                })
                .ToListAsync();

            return Ok(overrides);
        }
    }
}