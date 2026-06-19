using TrafficBrainAPI.Data;
using TrafficBrainAPI.Models;

namespace TrafficBrainAPI.Services
{
    public interface ISmsService
    {
        Task<bool> SendEmergencyAlertAsync(string phone, string junctionName, string vehicleType, string direction);
        Task<bool> SendIncidentAlertAsync(string phone, string junctionName, string incidentType, string severity);
        Task<bool> SendCongestionAlertAsync(string phone, string junctionName, int congestionScore);
        Task<bool> SendCustomMessageAsync(string phone, string message);
        Task SendBulkEmergencyAlertAsync(List<string> phones, string junctionName, string vehicleType);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ILogger<SmsService> _logger;
        private readonly HttpClient _httpClient;

        public SmsService(IConfiguration config, AppDbContext context,
            ILogger<SmsService> logger, HttpClient httpClient)
        {
            _config = config;
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<bool> SendEmergencyAlertAsync(
            string phone, string junctionName, string vehicleType, string direction)
        {
            var message = $"TRAFFICBRAIN EMERGENCY: {vehicleType} detected at {junctionName} " +
                         $"heading {direction}. Corridor being cleared. " +
                         $"Please avoid the area. - Malawi Traffic Control";

            return await SendSmsAsync(phone, message, "Emergency");
        }

        public async Task<bool> SendIncidentAlertAsync(
            string phone, string junctionName, string incidentType, string severity)
        {
            var message = $"TRAFFICBRAIN ALERT: {severity} {incidentType} reported at " +
                         $"{junctionName}. Expect delays. Use alternative routes. " +
                         $"- Malawi Traffic Control";

            return await SendSmsAsync(phone, message, "Incident");
        }

        public async Task<bool> SendCongestionAlertAsync(
            string phone, string junctionName, int congestionScore)
        {
            var level = congestionScore >= 75 ? "GRIDLOCK" :
                       congestionScore >= 50 ? "HEAVY" : "MODERATE";

            var message = $"TRAFFICBRAIN: {level} congestion at {junctionName} " +
                         $"(Score: {congestionScore}/100). " +
                         $"Consider alternative routes. - Malawi Traffic Control";

            return await SendSmsAsync(phone, message, "Congestion");
        }

        public async Task<bool> SendCustomMessageAsync(string phone, string message)
        {
            return await SendSmsAsync(phone, message, "Custom");
        }

        public async Task SendBulkEmergencyAlertAsync(
            List<string> phones, string junctionName, string vehicleType)
        {
            var tasks = phones.Select(phone =>
                SendEmergencyAlertAsync(phone, junctionName, vehicleType, "Unknown"));
            await Task.WhenAll(tasks);
        }

        private async Task<bool> SendSmsAsync(string phone, string message, string alertType)
        {
            var smsLog = new SmsAlert
            {
                RecipientPhone = phone,
                Message = message,
                AlertType = alertType,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var username = _config["AfricasTalking:Username"] ?? "sandbox";
                var apiKey = _config["AfricasTalking:ApiKey"] ?? "";
                var senderId = _config["AfricasTalking:SenderId"] ?? "TRAFFICBRAIN";

                // Africa's Talking API call via HTTP
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("to", phone),
                    new KeyValuePair<string, string>("message", message),
                    new KeyValuePair<string, string>("from", senderId)
                });

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apiKey", apiKey);
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await _httpClient.PostAsync(
                    "https://api.sandbox.africastalking.com/version1/messaging",
                    formData);

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"AT Response: {responseBody}");

                smsLog.Status = response.IsSuccessStatusCode ? "Sent" : "Failed";
                smsLog.SentAt = DateTime.UtcNow;

                _logger.LogInformation($"SMS {smsLog.Status} to {phone}: {alertType}");

                _context.SmsAlerts.Add(smsLog);
                await _context.SaveChangesAsync();

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SMS failed to {phone}: {ex.Message}");

                smsLog.Status = "Failed";
                _context.SmsAlerts.Add(smsLog);
                await _context.SaveChangesAsync();

                return false;
            }
        }
    }
}