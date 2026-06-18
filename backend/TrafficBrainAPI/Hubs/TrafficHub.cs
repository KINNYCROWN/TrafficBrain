using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace TrafficBrainAPI.Hubs
{
    public class TrafficHub : Hub
    {
        // ─────────────────────────────────────────
        // CONNECTION EVENTS
        // ─────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("Connected", new
            {
                Message = "Connected to TRAFFICBRAIN hub",
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.UtcNow
            });
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        // ─────────────────────────────────────────
        // GROUP MANAGEMENT
        // ─────────────────────────────────────────
        public async Task JoinJunctionGroup(string junctionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"junction_{junctionId}");
            await Clients.Caller.SendAsync("JoinedGroup", $"Subscribed to junction {junctionId}");
        }

        public async Task LeaveJunctionGroup(string junctionId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"junction_{junctionId}");
        }

        public async Task JoinDistrictGroup(string district)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"district_{district}");
            await Clients.Caller.SendAsync("JoinedGroup", $"Subscribed to district {district}");
        }

        // ─────────────────────────────────────────
        // VEHICLE DATA
        // ─────────────────────────────────────────
        public async Task BroadcastVehicleCount(object data)
        {
            await Clients.All.SendAsync("VehicleCountUpdated", data);
        }

        // ─────────────────────────────────────────
        // SIGNAL CONTROL
        // ─────────────────────────────────────────
        public async Task BroadcastSignalChange(object data)
        {
            await Clients.All.SendAsync("SignalPhaseChanged", data);
        }

        public async Task BroadcastJunctionOverride(object data)
        {
            await Clients.All.SendAsync("JunctionOverrideActivated", data);
        }

        public async Task BroadcastOverrideDeactivated(object data)
        {
            await Clients.All.SendAsync("JunctionOverrideDeactivated", data);
        }

        // ─────────────────────────────────────────
        // INCIDENTS & ALERTS
        // ─────────────────────────────────────────
        public async Task BroadcastIncident(object data)
        {
            await Clients.All.SendAsync("IncidentDetected", data);
        }

        public async Task BroadcastIncidentResolved(object data)
        {
            await Clients.All.SendAsync("IncidentResolved", data);
        }

        public async Task BroadcastAlert(object data)
        {
            await Clients.All.SendAsync("AlertReceived", data);
        }

        public async Task AcknowledgeAlert(int alertId)
        {
            await Clients.All.SendAsync("AlertAcknowledged", new
            {
                AlertId = alertId,
                Timestamp = DateTime.UtcNow
            });
        }

        // ─────────────────────────────────────────
        // EMERGENCY
        // ─────────────────────────────────────────
        public async Task BroadcastEmergency(object data)
        {
            await Clients.All.SendAsync("EmergencyVehicleDetected", data);
        }

        public async Task BroadcastCorridorCleared(object data)
        {
            await Clients.All.SendAsync("EmergencyCorridorCleared", data);
        }

        public async Task BroadcastEmergencyResolved(object data)
        {
            await Clients.All.SendAsync("EmergencyResolved", data);
        }

        // ─────────────────────────────────────────
        // SYSTEM HEALTH
        // ─────────────────────────────────────────
        public async Task BroadcastJunctionOnline(int junctionId)
        {
            await Clients.All.SendAsync("JunctionOnline", new
            {
                JunctionId = junctionId,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task BroadcastJunctionOffline(int junctionId)
        {
            await Clients.All.SendAsync("JunctionOffline", new
            {
                JunctionId = junctionId,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task BroadcastCongestionAlert(object data)
        {
            await Clients.All.SendAsync("CongestionAlert", data);
        }

        // ─────────────────────────────────────────
        // VIP CONVOY
        // ─────────────────────────────────────────
        public async Task BroadcastVIPCorridorActivated(object data)
        {
            await Clients.All.SendAsync("VIPCorridorActivated", data);
        }

        public async Task BroadcastVIPCorridorDeactivated(object data)
        {
            await Clients.All.SendAsync("VIPCorridorDeactivated", data);
        }
    }
}