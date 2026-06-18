import * as signalR from '@microsoft/signalr';

const HUB_URL = 'http://localhost:5275/traffichub';

class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.listeners = {};
  }

  async connect() {
    const token = localStorage.getItem('accessToken');

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Register all event listeners
    this.registerEvents();

    try {
      await this.connection.start();
      this.isConnected = true;
      console.log('✅ Connected to TRAFFICBRAIN hub');
      return true;
    } catch (err) {
      console.error('❌ SignalR connection failed:', err);
      this.isConnected = false;
      return false;
    }
  }

  registerEvents() {
    // ── Vehicle Data ──
    this.connection.on('VehicleCountUpdated', (data) => {
      this.emit('VehicleCountUpdated', data);
    });

    // ── Signal Control ──
    this.connection.on('SignalPhaseChanged', (data) => {
      this.emit('SignalPhaseChanged', data);
    });

    this.connection.on('JunctionOverrideActivated', (data) => {
      this.emit('JunctionOverrideActivated', data);
    });

    this.connection.on('JunctionOverrideDeactivated', (data) => {
      this.emit('JunctionOverrideDeactivated', data);
    });

    // ── Incidents & Alerts ──
    this.connection.on('IncidentDetected', (data) => {
      this.emit('IncidentDetected', data);
    });

    this.connection.on('IncidentResolved', (data) => {
      this.emit('IncidentResolved', data);
    });

    this.connection.on('AlertReceived', (data) => {
      this.emit('AlertReceived', data);
    });

    this.connection.on('AlertAcknowledged', (data) => {
      this.emit('AlertAcknowledged', data);
    });

    // ── Emergency ──
    this.connection.on('EmergencyVehicleDetected', (data) => {
      this.emit('EmergencyVehicleDetected', data);
    });

    this.connection.on('EmergencyCorridorCleared', (data) => {
      this.emit('EmergencyCorridorCleared', data);
    });

    this.connection.on('EmergencyResolved', (data) => {
      this.emit('EmergencyResolved', data);
    });

    // ── Congestion ──
    this.connection.on('CongestionAlert', (data) => {
      this.emit('CongestionAlert', data);
    });

    // ── System ──
    this.connection.on('JunctionOnline', (data) => {
      this.emit('JunctionOnline', data);
    });

    this.connection.on('JunctionOffline', (data) => {
      this.emit('JunctionOffline', data);
    });

    // ── VIP ──
    this.connection.on('VIPCorridorActivated', (data) => {
      this.emit('VIPCorridorActivated', data);
    });

    this.connection.on('VIPCorridorDeactivated', (data) => {
      this.emit('VIPCorridorDeactivated', data);
    });

    // ── Connection events ──
    this.connection.onreconnecting(() => {
      this.isConnected = false;
      this.emit('ConnectionChanged', { connected: false, status: 'Reconnecting' });
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      this.emit('ConnectionChanged', { connected: true, status: 'Connected' });
    });

    this.connection.onclose(() => {
      this.isConnected = false;
      this.emit('ConnectionChanged', { connected: false, status: 'Disconnected' });
    });
  }

  // Subscribe to a junction group
  async joinJunction(junctionId) {
    if (this.isConnected) {
      await this.connection.invoke('JoinJunctionGroup', junctionId.toString());
    }
  }

  // Unsubscribe from a junction group
  async leaveJunction(junctionId) {
    if (this.isConnected) {
      await this.connection.invoke('LeaveJunctionGroup', junctionId.toString());
    }
  }

  // Event emitter pattern
  on(event, callback) {
    if (!this.listeners[event]) {
      this.listeners[event] = [];
    }
    this.listeners[event].push(callback);
  }

  off(event, callback) {
    if (this.listeners[event]) {
      this.listeners[event] = this.listeners[event]
        .filter(cb => cb !== callback);
    }
  }

  emit(event, data) {
    if (this.listeners[event]) {
      this.listeners[event].forEach(cb => cb(data));
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop();
      this.isConnected = false;
    }
  }
}

// Singleton instance
const signalRService = new SignalRService();
export default signalRService;