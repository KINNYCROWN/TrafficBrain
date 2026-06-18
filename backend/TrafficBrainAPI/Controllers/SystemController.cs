using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Data;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private static readonly DateTime _startTime = DateTime.UtcNow;

        public SystemController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/system/health
        [HttpGet("health")]
        public async Task<ActionResult> GetHealth()
        {
            bool dbConnected = false;
            try
            {
                await _context.Database.CanConnectAsync();
                dbConnected = true;
            }
            catch { }

            var uptime = DateTime.UtcNow - _startTime;

            return Ok(new
            {
                Status = dbConnected ? "Healthy" : "Degraded",
                Database = dbConnected ? "Connected" : "Disconnected",
                Uptime = $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s",
                UptimeSeconds = (int)uptime.TotalSeconds,
                Version = "2.0.0",
                Environment = _config["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                Timestamp = DateTime.UtcNow
            });
        }

        // GET: api/system/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetSystemStats()
        {
            var totalUsers = await _context.Users.CountAsync(u => u.IsActive);
            var totalJunctions = await _context.Junctions.CountAsync(j => j.IsActive);
            var onlineJunctions = await _context.Junctions.CountAsync(j => j.IsOnline);
            var totalVehicleRecords = await _context.VehicleCounts.CountAsync();
            var totalIncidents = await _context.Incidents.CountAsync();
            var totalEmergencies = await _context.EmergencyEvents.CountAsync();
            var totalAlerts = await _context.Alerts.CountAsync();
            var totalLogs = await _context.SystemLogs.CountAsync();
            var uptime = DateTime.UtcNow - _startTime;

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalJunctions = totalJunctions,
                OnlineJunctions = onlineJunctions,
                TotalVehicleRecords = totalVehicleRecords,
                TotalIncidents = totalIncidents,
                TotalEmergencies = totalEmergencies,
                TotalAlerts = totalAlerts,
                TotalLogs = totalLogs,
                UptimeSeconds = (int)uptime.TotalSeconds,
                Timestamp = DateTime.UtcNow
            });
        }

        // GET: api/system/logs
        [HttpGet("logs")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? action = null,
            [FromQuery] string? role = null)
        {
            var query = _context.SystemLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (!string.IsNullOrEmpty(role))
                query = query.Where(l => l.UserRole == role);

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.JunctionId,
                    l.Details,
                    l.IpAddress,
                    l.UserRole,
                    l.Timestamp,
                    UserName = l.User != null ? l.User.FullName : "System",
                    UserEmail = l.User != null ? l.User.Email : ""
                })
                .ToListAsync();

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                Logs = logs
            });
        }

        // GET: api/system/junctions/status
        [HttpGet("junctions/status")]
        public async Task<ActionResult> GetJunctionStatus()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);

            var junctions = await _context.Junctions
                .Include(j => j.District)
                .Where(j => j.IsActive)
                .Select(j => new
                {
                    j.Id,
                    j.Name,
                    District = j.District != null ? j.District.Name : "Unknown",
                    j.IsOnline,
                    j.ControlMode,
                    j.LastDataReceived,
                    Status = j.LastDataReceived >= cutoff ? "Online" :
                             j.LastDataReceived == null ? "Never Connected" : "Offline",
                    MinutesSinceLastData = j.LastDataReceived.HasValue
                        ? (int)(DateTime.UtcNow - j.LastDataReceived.Value).TotalMinutes
                        : -1
                })
                .ToListAsync();

            return Ok(new
            {
                Total = junctions.Count,
                Online = junctions.Count(j => j.Status == "Online"),
                Offline = junctions.Count(j => j.Status == "Offline"),
                NeverConnected = junctions.Count(j => j.Status == "Never Connected"),
                Junctions = junctions
            });
        }

        // GET: api/system/districts
        [HttpGet("districts")]
        public async Task<ActionResult> GetDistricts()
        {
            var districts = await _context.Districts
                .Include(d => d.Region)
                .Where(d => d.IsActive)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    Region = d.Region != null ? d.Region.Name : "Unknown"
                })
                .ToListAsync();

            return Ok(districts);
        }

        // GET: api/system/regions
        [HttpGet("regions")]
        public async Task<ActionResult> GetRegions()
        {
            var regions = await _context.Regions
                .Include(r => r.Districts)
                .Where(r => r.IsActive)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    Districts = r.Districts
                        .Where(d => d.IsActive)
                        .Select(d => new { d.Id, d.Name })
                })
                .ToListAsync();

            return Ok(regions);
        }
    }
}