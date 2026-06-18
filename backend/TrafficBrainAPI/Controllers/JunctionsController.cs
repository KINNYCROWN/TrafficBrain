using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.Models;
using TrafficBrainAPI.Hubs;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/junctions")]
    public class JunctionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<TrafficHub> _hub;

        public JunctionsController(AppDbContext context, IHubContext<TrafficHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        // GET: api/junctions
        [HttpGet]
        public async Task<ActionResult> GetJunctions()
        {
            var junctions = await _context.Junctions
                .Include(j => j.District)
                .Include(j => j.City)
                .Where(j => j.IsActive)
                .OrderBy(j => j.District!.Name)
                .Select(j => new
                {
                    j.Id,
                    j.Name,
                    j.RoadNames,
                    j.Latitude,
                    j.Longitude,
                    j.ControlMode,
                    j.IsOnline,
                    j.LastDataReceived,
                    District = j.District!.Name,
                    City = j.City!.Name,
                    j.CreatedAt
                })
                .ToListAsync();

            return Ok(junctions);
        }

        // GET: api/junctions/5
        [HttpGet("{id}")]
        public async Task<ActionResult> GetJunction(int id)
        {
            var junction = await _context.Junctions
                .Include(j => j.District)
                .Include(j => j.City)
                .Include(j => j.Lanes)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (junction == null) return NotFound();

            return Ok(new
            {
                junction.Id,
                junction.Name,
                junction.RoadNames,
                junction.Latitude,
                junction.Longitude,
                junction.ControlMode,
                junction.IsOnline,
                junction.LastDataReceived,
                District = junction.District?.Name,
                City = junction.City?.Name,
                Lanes = junction.Lanes.Select(l => new
                {
                    l.Id,
                    l.Direction,
                    l.LaneNumber,
                    l.IsActive
                })
            });
        }

        // GET: api/junctions/district/Lilongwe
        [HttpGet("district/{district}")]
        public async Task<ActionResult> GetByDistrict(string district)
        {
            var junctions = await _context.Junctions
                .Include(j => j.District)
                .Include(j => j.City)
                .Where(j => j.District!.Name.ToLower() == district.ToLower() && j.IsActive)
                .Select(j => new
                {
                    j.Id,
                    j.Name,
                    j.RoadNames,
                    j.Latitude,
                    j.Longitude,
                    j.ControlMode,
                    j.IsOnline,
                    District = j.District!.Name,
                    City = j.City!.Name
                })
                .ToListAsync();

            return Ok(junctions);
        }

        // POST: api/junctions
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CreateJunction(Junction junction)
        {
            _context.Junctions.Add(junction);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetJunction), new { id = junction.Id }, junction);
        }

        // PUT: api/junctions/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateJunction(int id, Junction updated)
        {
            var junction = await _context.Junctions.FindAsync(id);
            if (junction == null) return NotFound();

            junction.Name = updated.Name;
            junction.RoadNames = updated.RoadNames;
            junction.Latitude = updated.Latitude;
            junction.Longitude = updated.Longitude;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Junction updated" });
        }

        // DELETE: api/junctions/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeactivateJunction(int id)
        {
            var junction = await _context.Junctions.FindAsync(id);
            if (junction == null) return NotFound();

            junction.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Junction {junction.Name} deactivated" });
        }

        // GET: api/junctions/online
        [HttpGet("online")]
        public async Task<ActionResult> GetOnlineJunctions()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var junctions = await _context.Junctions
                .Include(j => j.District)
                .Where(j => j.IsActive && j.LastDataReceived >= cutoff)
                .Select(j => new
                {
                    j.Id,
                    j.Name,
                    District = j.District!.Name,
                    j.IsOnline,
                    j.LastDataReceived,
                    j.ControlMode
                })
                .ToListAsync();

            return Ok(junctions);
        }

        // GET: api/junctions/stats
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            var totalJunctions = await _context.Junctions.CountAsync(j => j.IsActive);
            var onlineJunctions = await _context.Junctions
                .CountAsync(j => j.IsOnline && j.IsActive);
            var totalVehiclesLastHour = await _context.VehicleCounts
                .Where(v => v.RecordedAt >= DateTime.UtcNow.AddHours(-1))
                .SumAsync(v => v.TotalVehicles);
            var activeIncidents = await _context.Incidents
                .CountAsync(i => !i.IsResolved);
            var districts = await _context.Districts.CountAsync(d => d.IsActive);
            var activeEmergencies = await _context.EmergencyEvents
                .CountAsync(e => !e.CorridorCleared);
            var unacknowledgedAlerts = await _context.Alerts
                .CountAsync(a => !a.IsAcknowledged);

            return Ok(new
            {
                TotalJunctions = totalJunctions,
                OnlineJunctions = onlineJunctions,
                OfflineJunctions = totalJunctions - onlineJunctions,
                TotalVehiclesLastHour = totalVehiclesLastHour,
                ActiveIncidents = activeIncidents,
                ActiveEmergencies = activeEmergencies,
                UnacknowledgedAlerts = unacknowledgedAlerts,
                DistrictsCovered = districts,
                LastUpdated = DateTime.UtcNow
            });
        }
    }
}