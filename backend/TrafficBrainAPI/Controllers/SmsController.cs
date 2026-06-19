using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TrafficBrainAPI.Data;
using TrafficBrainAPI.DTOs;
using TrafficBrainAPI.Services;

namespace TrafficBrainAPI.Controllers
{
    [ApiController]
    [Route("api/sms")]
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;
        private readonly AppDbContext _context;

        public SmsController(ISmsService smsService, AppDbContext context)
        {
            _smsService = smsService;
            _context = context;
        }

        // POST: api/sms/emergency
        [HttpPost("emergency")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> SendEmergencyAlert([FromBody] SmsAlertRequest request)
        {
            var success = await _smsService.SendEmergencyAlertAsync(
                request.RecipientPhone,
                request.RecipientName,
                "Emergency Vehicle",
                request.Message
            );

            return Ok(new
            {
                success,
                message = success ? "Emergency SMS sent" : "SMS failed — check logs"
            });
        }

        // POST: api/sms/incident
        [HttpPost("incident")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> SendIncidentAlert([FromBody] SmsAlertRequest request)
        {
            var success = await _smsService.SendIncidentAlertAsync(
                request.RecipientPhone,
                request.RecipientName,
                request.AlertType,
                "High"
            );

            return Ok(new
            {
                success,
                message = success ? "Incident SMS sent" : "SMS failed — check logs"
            });
        }

        // POST: api/sms/congestion
        [HttpPost("congestion")]
        [Authorize(Roles = "Admin,Supervisor,TrafficOfficer")]
        public async Task<ActionResult> SendCongestionAlert([FromBody] SmsAlertRequest request)
        {
            var success = await _smsService.SendCongestionAlertAsync(
                request.RecipientPhone,
                request.RecipientName,
                75
            );

            return Ok(new
            {
                success,
                message = success ? "Congestion SMS sent" : "SMS failed — check logs"
            });
        }

        // POST: api/sms/custom
        [HttpPost("custom")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> SendCustomMessage([FromBody] SmsAlertRequest request)
        {
            var success = await _smsService.SendCustomMessageAsync(
                request.RecipientPhone,
                request.Message
            );

            return Ok(new
            {
                success,
                message = success ? "SMS sent" : "SMS failed — check logs"
            });
        }

        // POST: api/sms/bulk
        [HttpPost("bulk")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> SendBulkAlert([FromBody] List<SmsAlertRequest> requests)
        {
            var phones = requests.Select(r => r.RecipientPhone).ToList();
            var firstRequest = requests.FirstOrDefault();

            if (firstRequest == null)
                return BadRequest(new { error = "No recipients provided" });

            await _smsService.SendBulkEmergencyAlertAsync(
                phones,
                firstRequest.RecipientName,
                firstRequest.AlertType
            );

            return Ok(new
            {
                message = $"Bulk SMS sent to {phones.Count} recipients"
            });
        }

        // GET: api/sms/logs
        [HttpGet("logs")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<ActionResult> GetSmsLogs()
        {
            var logs = await _context.SmsAlerts
                .OrderByDescending(s => s.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(logs);
        }
    }
}