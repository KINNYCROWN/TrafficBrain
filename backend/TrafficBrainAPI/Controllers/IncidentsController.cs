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
    [Route("api/incidents")]
    public class IncidentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TrafficHub> _hub;

        public IncidentsController(AppDbContext context, IHubContext<TrafficHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // POST: api/incidents
        [HttpPost]
        public async Task<ActionResult> ReportIncident(IncidentRequest request)
        {
            var junction = await _context.Junctions
                .Include(j => j.District)
                .FirstOrDefaultAsync(j => j.Id == request.JunctionId);

            if (junction == null)
                return BadRequest(new { error = "Junction not found" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var incident = new Incident
            {
                JunctionId = request.JunctionId,
                ReportedById = userId != null ? int.Parse(userId) : null,
                Type = request.Type,
                Description = request.Description,
                Severity = request.Severity,
                DetectedAt = DateTime.UtcNow
            };

            _context.Incidents.Add(incident);

            // Create alert
            _context.Alerts.Add(new Alert
            {
                JunctionId = request.JunctionId,
                AlertType = "Incident",
                Message = $"{request.Severity} {request.Type} at {junction.Name}: {request.Description}",
                Severity = request.Severity,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Broadcast to dashboard
            await _hub.Clients.All.SendAsync("IncidentDetected", new
            {
                incident.Id,
                incident.JunctionId,
                JunctionName = junction.Name,
                District = junction.District?.Name,
                incident.Type,
                incident.Description,
                incident.Severity,
                incident.DetectedAt
            });

            return Ok(new { message = "Incident reported", IncidentId = incident.Id });
        }

        // GET: api/incidents/active
        [HttpGet("active")]
        public async Task<ActionResult> GetActiveIncidents()
        {
            var incidents = await _context.Incidents
                .Include(i => i.Junction)
                    .ThenInclude(j => j!.District)
                .Where(i => !i.IsResolved)
                .OrderByDescending(i => i.DetectedAt)
                .Select(i => new
                {
                    i.Id,
                    i.JunctionId,
                    JunctionName = i.Junction != null ? i.Junction.Name : "Unknown",
                    District = i.Junction != null && i.Junction.District != null
                        ? i.Junction.District.Name : "Unknown",
                    i.Type,
                    i.Description,
                    i.Severity,
                    i.IsResolved,
                    i.DetectedAt
                })
                .ToListAsync();

            return Ok(incidents);
        }

        // PUT: api/incidents/5/resolve
        [HttpPut("{id}/resolve")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> ResolveIncident(int id)
        {
            var incident = await _context.Incidents
                .Include(i => i.Junction)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (incident == null) return NotFound();

            var officerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            incident.IsResolved = true;
            incident.ResolvedAt = DateTime.UtcNow;
            incident.ResolvedBy = officerName;

            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("IncidentResolved", new
            {
                IncidentId = id,
                incident.JunctionId,
                JunctionName = incident.Junction?.Name,
                ResolvedBy = officerName,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Incident resolved" });
        }

        // GET: api/incidents/history
        [HttpGet("history")]
        public async Task<ActionResult> GetIncidentHistory(
            [FromQuery] string? district = null,
            [FromQuery] int days = 7)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var query = _context.Incidents
                .Include(i => i.Junction)
                    .ThenInclude(j => j!.District)
                .Where(i => i.DetectedAt >= since);

            if (!string.IsNullOrEmpty(district))
            {
                query = query.Where(i =>
                    i.Junction != null &&
                    i.Junction.District != null &&
                    i.Junction.District.Name.ToLower() == district.ToLower());
            }

            var incidents = await query
                .OrderByDescending(i => i.DetectedAt)
                .Select(i => new
                {
                    i.Id,
                    i.JunctionId,
                    JunctionName = i.Junction != null ? i.Junction.Name : "Unknown",
                    District = i.Junction != null && i.Junction.District != null
                        ? i.Junction.District.Name : "Unknown",
                    i.Type,
                    i.Description,
                    i.Severity,
                    i.IsResolved,
                    i.ResolvedBy,
                    i.DetectedAt,
                    i.ResolvedAt
                })
                .ToListAsync();

            return Ok(incidents);
        }

        // GET: api/incidents/alerts
        [HttpGet("alerts")]
        public async Task<ActionResult> GetAlerts()
        {
            var alerts = await _context.Alerts
                .Include(a => a.Junction)
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .Select(a => new
                {
                    a.Id,
                    a.JunctionId,
                    JunctionName = a.Junction != null ? a.Junction.Name : "Unknown",
                    a.AlertType,
                    a.Message,
                    a.Severity,
                    a.IsAcknowledged,
                    a.AcknowledgedBy,
                    a.AcknowledgedAt,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        // PUT: api/incidents/alerts/5/acknowledge
        [HttpPut("alerts/{id}/acknowledge")]
        [Authorize]
        public async Task<ActionResult> AcknowledgeAlert(int id)
        {
            var alert = await _context.Alerts.FindAsync(id);
            if (alert == null) return NotFound();

            var officerName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

            alert.IsAcknowledged = true;
            alert.AcknowledgedBy = officerName;
            alert.AcknowledgedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("AlertAcknowledged", new
            {
                AlertId = id,
                AcknowledgedBy = officerName,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { message = "Alert acknowledged" });
        }
    }
}