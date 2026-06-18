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
    [Route("api/emergency")]
    public class EmergencyController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TrafficHub> _hub;

        public EmergencyController(AppDbContext context, IHubContext<TrafficHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // POST: api/emergency/detect
        [HttpPost("detect")]
        public async Task<ActionResult> DetectEmergency(EmergencyRequest request)
        {
            var junction = await _context.Junctions
                .Include(j => j.District)
                .FirstOrDefaultAsync(j => j.Id == request.JunctionId);

            if (junction == null)
                return BadRequest(new { error = "Junction not found" });

            // Find corridor junctions
            var corridorJunctions = await GetCorridorJunctions(request.JunctionId, request.Direction);
            var corridorIds = string.Join(",", corridorJunctions.Select(j => j.Id));

            var emergencyEvent = new EmergencyEvent
            {
                JunctionId = request.JunctionId,
                VehicleType = request.VehicleType,
                Direction = request.Direction,
                CorridorJunctions = corridorIds,
                CorridorCleared = false,
                DetectedAt = DateTime.UtcNow
            };

            _context.EmergencyEvents.Add(emergencyEvent);

            // Create alert
            _context.Alerts.Add(new Alert
            {
                JunctionId = request.JunctionId,
                AlertType = "Emergency",
                Message = $"Emergency {request.VehicleType} detected at {junction.Name} — Corridor being cleared",
                Severity = "Critical",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Broadcast emergency to dashboard
            await _hub.Clients.All.SendAsync("EmergencyVehicleDetected", new
            {
                EmergencyId = emergencyEvent.Id,
                request.JunctionId,
                JunctionName = junction.Name,
                District = junction.District?.Name,
                request.VehicleType,
                request.Direction,
                CorridorJunctions = corridorJunctions.Select(j => new
                {
                    j.Id,
                    j.Name,
                    j.Latitude,
                    j.Longitude
                }),
                Message = $"Emergency {request.VehicleType} — Clearing corridor through {corridorJunctions.Count} junctions",
                Timestamp = DateTime.UtcNow
            });

            // Simulate corridor clearing
            await Task.Delay(2000);

            emergencyEvent.CorridorCleared = true;
            emergencyEvent.TimeSavedMinutes = corridorJunctions.Count * 2.5;
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("EmergencyCorridorCleared", new
            {
                EmergencyId = emergencyEvent.Id,
                request.JunctionId,
                JunctionName = junction.Name,
                CorridorJunctions = corridorJunctions.Count,
                TimeSavedMinutes = emergencyEvent.TimeSavedMinutes,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new
            {
                message = "Emergency detected and corridor cleared",
                EmergencyId = emergencyEvent.Id,
                CorridorJunctions = corridorJunctions.Count,
                TimeSavedMinutes = emergencyEvent.TimeSavedMinutes
            });
        }

        // GET: api/emergency/active
        [HttpGet("active")]
        public async Task<ActionResult> GetActiveEmergencies()
        {
            var emergencies = await _context.EmergencyEvents
                .Include(e => e.Junction)
                .Where(e => !e.CorridorCleared)
                .OrderByDescending(e => e.DetectedAt)
                .Select(e => new
                {
                    e.Id,
                    e.JunctionId,
                    JunctionName = e.Junction != null ? e.Junction.Name : "Unknown",
                    e.VehicleType,
                    e.Direction,
                    e.CorridorCleared,
                    e.DetectedAt
                })
                .ToListAsync();

            return Ok(emergencies);
        }

        // PUT: api/emergency/5/resolve
        [HttpPut("{id}/resolve")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> ResolveEmergency(int id)
        {
            var emergency = await _context.EmergencyEvents.FindAsync(id);
            if (emergency == null) return NotFound();

            emergency.CorridorCleared = true;
            emergency.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("EmergencyResolved", new
            {
                EmergencyId = id,
                emergency.JunctionId,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Emergency resolved" });
        }

        // GET: api/emergency/history
        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult> GetEmergencyHistory()
        {
            var history = await _context.EmergencyEvents
                .Include(e => e.Junction)
                .OrderByDescending(e => e.DetectedAt)
                .Take(50)
                .Select(e => new
                {
                    e.Id,
                    e.JunctionId,
                    JunctionName = e.Junction != null ? e.Junction.Name : "Unknown",
                    e.VehicleType,
                    e.Direction,
                    e.CorridorCleared,
                    e.TimeSavedMinutes,
                    e.DetectedAt,
                    e.ResolvedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // POST: api/emergency/vip
        [HttpPost("vip")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> ActivateVIPCorridor(VIPCorridorRequest request)
        {
            var officerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            await _hub.Clients.All.SendAsync("VIPCorridorActivated", new
            {
                JunctionIds = request.JunctionIds,
                Reason = request.Reason,
                HoldDurationSeconds = request.HoldDurationSeconds,
                ActivatedBy = officerName,
                Timestamp = DateTime.UtcNow
            });

            // Log the VIP corridor
            var officerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = officerId,
                Action = "VIP_CORRIDOR",
                Details = $"VIP corridor activated across {request.JunctionIds.Count} junctions. Reason: {request.Reason}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown",
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"VIP corridor activated across {request.JunctionIds.Count} junctions",
                JunctionIds = request.JunctionIds,
                HoldDurationSeconds = request.HoldDurationSeconds
            });
        }

        // Helper: Get corridor junctions based on direction
        private async Task<List<Junction>> GetCorridorJunctions(int startJunctionId, string direction)
        {
            var startJunction = await _context.Junctions
                .Include(j => j.District)
                .FirstOrDefaultAsync(j => j.Id == startJunctionId);

            if (startJunction == null) return new List<Junction>();

            // Get nearby junctions in the same district
            var nearbyJunctions = await _context.Junctions
                .Include(j => j.District)
                .Where(j => j.DistrictId == startJunction.DistrictId
                    && j.Id != startJunctionId
                    && j.IsActive)
                .OrderBy(j => Math.Abs(j.Latitude - startJunction.Latitude)
                            + Math.Abs(j.Longitude - startJunction.Longitude))
                .Take(3)
                .ToListAsync();

            var corridor = new List<Junction> { startJunction };
            corridor.AddRange(nearbyJunctions);

            return corridor;
        }
    }
}