import React, { useState, useEffect } from 'react';
import { MapContainer, TileLayer, CircleMarker, Popup } from 'react-leaflet';
import { useNavigate } from 'react-router-dom';
import { junctionsAPI } from '../services/api';
import signalRService from '../services/signalr';
import Layout from '../components/Layout';
import 'leaflet/dist/leaflet.css';

const getCongestionColor = (score) => {
  if (score === undefined || score === null) return '#3388ff';
  if (score >= 75) return '#ef4444';
  if (score >= 50) return '#f97316';
  if (score >= 25) return '#eab308';
  return '#22c55e';
};

const getCongestionLabel = (score) => {
  if (score === undefined || score === null) return 'No Data';
  if (score >= 75) return 'GRIDLOCK';
  if (score >= 50) return 'HEAVY';
  if (score >= 25) return 'MODERATE';
  return 'FREE FLOW';
};

export default function National() {
  const [junctions, setJunctions] = useState([]);
  const [stats, setStats] = useState(null);
  const [vehicleCounts, setVehicleCounts] = useState({});
  const [incidents, setIncidents] = useState([]);
  const [emergencies, setEmergencies] = useState([]);
  const [alerts, setAlerts] = useState([]);
  const [isConnected, setIsConnected] = useState(false);
  const [selectedDistrict, setSelectedDistrict] = useState('All');
  const [currentTime, setCurrentTime] = useState(new Date());
  const navigate = useNavigate();

  useEffect(() => {
    const timer = setInterval(() => setCurrentTime(new Date()), 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    junctionsAPI.getAll().then(res => setJunctions(res.data)).catch(console.error);
    junctionsAPI.getStats().then(res => setStats(res.data)).catch(console.error);
    const interval = setInterval(() => {
      junctionsAPI.getStats().then(res => setStats(res.data)).catch(console.error);
    }, 10000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    const connect = async () => {
      const connected = await signalRService.connect();
      setIsConnected(connected);
    };
    connect();

    signalRService.on('VehicleCountUpdated', (data) => {
      setVehicleCounts(prev => ({ ...prev, [data.junctionId]: data }));
    });
    signalRService.on('IncidentDetected', (data) => {
      setIncidents(prev => [data, ...prev].slice(0, 10));
    });
    signalRService.on('EmergencyVehicleDetected', (data) => {
      setEmergencies(prev => [data, ...prev].slice(0, 5));
    });
    signalRService.on('CongestionAlert', (data) => {
      setAlerts(prev => [data, ...prev].slice(0, 5));
    });
    signalRService.on('ConnectionChanged', ({ connected }) => {
      setIsConnected(connected);
    });

    return () => signalRService.disconnect();
  }, []);

  const districts = ['All', ...new Set(junctions.map(j => j.district))];
  const filteredJunctions = selectedDistrict === 'All'
    ? junctions
    : junctions.filter(j => j.district === selectedDistrict);

  return (
    <Layout>
      <div style={styles.container}>

        {/* STATUS BAR */}
        <div style={styles.statusBar}>
          <div style={styles.statusLeft}>
            <span style={styles.clock}>
              {currentTime.toLocaleTimeString('en-GB')}
            </span>
            <span style={styles.date}>
              {currentTime.toLocaleDateString('en-GB', {
                weekday: 'short', day: 'numeric', month: 'short', year: 'numeric'
              })}
            </span>
            <span style={{
              ...styles.liveStatus,
              background: isConnected ? 'rgba(34,197,94,0.1)' : 'rgba(239,68,68,0.1)',
              border: `1px solid ${isConnected ? '#22c55e' : '#ef4444'}`,
              color: isConnected ? '#22c55e' : '#ef4444'
            }}>
              {isConnected ? '● LIVE' : '○ OFFLINE'}
            </span>
          </div>
        </div>

        {/* STATS BAR */}
        {stats && (
          <div style={styles.statsBar}>
            {[
              { value: stats.totalJunctions, label: 'Junctions', color: '#4da6ff' },
              { value: stats.onlineJunctions, label: 'Online', color: '#22c55e' },
              { value: stats.offlineJunctions, label: 'Offline', color: '#ef4444' },
              { value: stats.totalVehiclesLastHour, label: 'Vehicles/hr', color: '#f97316' },
              { value: stats.activeIncidents, label: 'Incidents', color: '#eab308' },
              { value: stats.activeEmergencies, label: 'Emergency', color: '#ef4444' },
              { value: stats.districtsCovered, label: 'Districts', color: '#a78bfa' },
              { value: stats.unacknowledgedAlerts, label: 'Alerts', color: '#f472b6' },
            ].map((stat, i) => (
              <div key={i} style={styles.stat}>
                <span style={{ ...styles.statValue, color: stat.color }}>
                  {stat.value ?? 0}
                </span>
                <span style={styles.statLabel}>{stat.label}</span>
              </div>
            ))}
          </div>
        )}

        {/* EMERGENCY BANNER */}
        {emergencies.length > 0 && (
          <div style={styles.emergencyBanner}>
            🚨 EMERGENCY: {emergencies[0].vehicleType} at{' '}
            {emergencies[0].junctionName} — Corridor clearing
          </div>
        )}

        {/* MAIN CONTENT */}
        <div style={styles.content}>

          {/* MAP */}
          <div style={styles.mapWrapper}>
            <div style={styles.filterBar}>
              {districts.map(d => (
                <button
                  key={d}
                  onClick={() => setSelectedDistrict(d)}
                  style={{
                    ...styles.filterBtn,
                    background: selectedDistrict === d
                      ? '#2563eb' : 'rgba(255,255,255,0.05)',
                    border: selectedDistrict === d
                      ? '1px solid #2563eb' : '1px solid rgba(255,255,255,0.1)'
                  }}>
                  {d}
                </button>
              ))}
            </div>

            <MapContainer
              center={[-13.2543, 34.3015]}
              zoom={7}
              style={{ height: '100%', width: '100%' }}
            >
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution='&copy; OpenStreetMap contributors'
              />
              {filteredJunctions.map(junction => {
                const count = vehicleCounts[junction.id];
                const score = count?.congestionScore;
                const color = getCongestionColor(score);
                return (
                  <CircleMarker
                    key={junction.id}
                    center={[junction.latitude, junction.longitude]}
                    radius={14}
                    fillColor={color}
                    color="#ffffff"
                    weight={2}
                    fillOpacity={0.85}
                    eventHandlers={{
                      click: () => navigate(`/junction/${junction.id}`)
                    }}
                  >
                    <Popup>
                      <div style={{ fontFamily: 'sans-serif', minWidth: '180px' }}>
                        <strong style={{ fontSize: '13px' }}>{junction.name}</strong>
                        <br />
                        <span style={{ color: '#666', fontSize: '11px' }}>
                          {junction.district} — {junction.roadNames}
                        </span>
                        <br /><br />
                        <div style={{
                          display: 'inline-block',
                          background: color,
                          color: '#fff',
                          padding: '2px 8px',
                          borderRadius: '4px',
                          fontSize: '11px',
                          fontWeight: 'bold'
                        }}>
                          {getCongestionLabel(score)}
                          {score !== undefined ? ` (${score}/100)` : ''}
                        </div>
                        {count && (
                          <div style={{ marginTop: '6px', fontSize: '11px' }}>
                            🚗 {count.cars || 0} &nbsp;
                            🏍️ {count.motorcycles || 0} &nbsp;
                            🚌 {count.buses || 0}
                          </div>
                        )}
                        <br />
                        <span style={{ color: '#2563eb', fontSize: '11px', cursor: 'pointer' }}>
                          Click for details →
                        </span>
                      </div>
                    </Popup>
                  </CircleMarker>
                );
              })}
            </MapContainer>

            {/* Legend */}
            <div style={styles.legend}>
              {[
                { color: '#22c55e', label: 'Free Flow' },
                { color: '#eab308', label: 'Moderate' },
                { color: '#f97316', label: 'Heavy' },
                { color: '#ef4444', label: 'Gridlock' },
                { color: '#3388ff', label: 'No Data' },
              ].map((item, i) => (
                <div key={i} style={styles.legendItem}>
                  <div style={{ ...styles.legendDot, background: item.color }} />
                  <span>{item.label}</span>
                </div>
              ))}
            </div>
          </div>

          {/* SIDE PANEL */}
          <div style={styles.sidePanel}>

            <div style={styles.panelSection}>
              <h3 style={styles.sectionTitle}>🗺️ Junctions</h3>
              <div style={styles.junctionList}>
                {filteredJunctions.map(junction => {
                  const count = vehicleCounts[junction.id];
                  const score = count?.congestionScore;
                  const color = getCongestionColor(score);
                  return (
                    <div
                      key={junction.id}
                      style={styles.junctionItem}
                      onClick={() => navigate(`/junction/${junction.id}`)}>
                      <div style={{
                        ...styles.junctionDot,
                        background: color,
                        boxShadow: `0 0 8px ${color}`
                      }} />
                      <div style={styles.junctionInfo}>
                        <span style={styles.junctionName}>{junction.name}</span>
                        <span style={styles.junctionMeta}>
                          {junction.district} •{' '}
                          {junction.isOnline ? '🟢' : '🔴'} •{' '}
                          {junction.controlMode}
                        </span>
                        {count && (
                          <span style={styles.junctionCount}>
                            {count.totalVehicles} vehicles | {score}/100
                          </span>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {incidents.length > 0 && (
              <div style={styles.panelSection}>
                <h3 style={{ ...styles.sectionTitle, color: '#eab308' }}>
                  ⚠️ Live Incidents
                </h3>
                {incidents.slice(0, 5).map((inc, i) => (
                  <div key={i} style={{
                    ...styles.alertItem,
                    borderLeft: `3px solid ${inc.severity === 'Critical'
                      ? '#ef4444' : inc.severity === 'High'
                      ? '#f97316' : '#eab308'}`
                  }}>
                    <strong style={{ fontSize: '12px' }}>{inc.type}</strong>
                    <span style={{ fontSize: '11px', color: '#9ca3af', display: 'block' }}>
                      {inc.junctionName} — {inc.severity}
                    </span>
                  </div>
                ))}
              </div>
            )}

            {alerts.length > 0 && (
              <div style={styles.panelSection}>
                <h3 style={{ ...styles.sectionTitle, color: '#f97316' }}>
                  🔴 Congestion Alerts
                </h3>
                {alerts.slice(0, 3).map((alert, i) => (
                  <div key={i} style={{
                    ...styles.alertItem,
                    borderLeft: '3px solid #f97316'
                  }}>
                    <strong style={{ fontSize: '12px' }}>{alert.junctionName}</strong>
                    <span style={{ fontSize: '11px', color: '#9ca3af', display: 'block' }}>
                      Score: {alert.congestionScore}/100
                    </span>
                  </div>
                ))}
              </div>
            )}

          </div>
        </div>
      </div>
    </Layout>
  );
}

const styles = {
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: 'calc(100vh - 52px)',
    overflow: 'hidden'
  },
  statusBar: {
    background: '#0d1b2a',
    borderBottom: '1px solid #1f2937',
    padding: '6px 16px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    flexShrink: 0,
    flexWrap: 'wrap',
    gap: '8px'
  },
  statusLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flexWrap: 'wrap'
  },
  clock: {
    fontSize: '15px',
    fontWeight: '700',
    color: '#4da6ff',
    fontFamily: 'monospace'
  },
  date: {
    fontSize: '12px',
    color: '#6b7280'
  },
  liveStatus: {
    fontSize: '11px',
    fontWeight: '700',
    padding: '3px 8px',
    borderRadius: '20px'
  },
  statsBar: {
    display: 'flex',
    background: '#111827',
    borderBottom: '1px solid #1f2937',
    padding: '6px 16px',
    gap: '20px',
    flexShrink: 0,
    overflowX: 'auto'
  },
  stat: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    minWidth: '60px'
  },
  statValue: {
    fontSize: '20px',
    fontWeight: '800',
    lineHeight: 1
  },
  statLabel: {
    fontSize: '9px',
    color: '#6b7280',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    marginTop: '2px',
    textAlign: 'center'
  },
  emergencyBanner: {
    background: 'linear-gradient(90deg, #7f1d1d, #991b1b)',
    border: '1px solid #ef4444',
    padding: '8px 16px',
    color: '#fca5a5',
    fontSize: '13px',
    fontWeight: '700',
    textAlign: 'center',
    flexShrink: 0
  },
  content: {
    display: 'flex',
    flex: 1,
    overflow: 'hidden'
  },
  mapWrapper: {
    flex: 1,
    position: 'relative',
    minHeight: '300px'
  },
  filterBar: {
    position: 'absolute',
    top: '10px',
    left: '50%',
    transform: 'translateX(-50%)',
    zIndex: 1000,
    display: 'flex',
    gap: '4px',
    background: 'rgba(10,14,26,0.9)',
    padding: '6px',
    borderRadius: '8px',
    border: '1px solid rgba(46,117,182,0.3)',
    flexWrap: 'wrap',
    justifyContent: 'center',
    maxWidth: '90%'
  },
  filterBtn: {
    padding: '4px 10px',
    borderRadius: '5px',
    color: '#ffffff',
    fontSize: '11px',
    cursor: 'pointer',
    fontWeight: '600'
  },
  legend: {
    position: 'absolute',
    bottom: '20px',
    left: '10px',
    zIndex: 1000,
    background: 'rgba(10,14,26,0.9)',
    padding: '8px 12px',
    borderRadius: '8px',
    border: '1px solid rgba(46,117,182,0.3)',
    display: 'flex',
    flexDirection: 'column',
    gap: '5px'
  },
  legendItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '6px',
    fontSize: '11px',
    color: '#9ca3af'
  },
  legendDot: {
    width: '10px',
    height: '10px',
    borderRadius: '50%',
    flexShrink: 0
  },
  sidePanel: {
    width: '280px',
    background: '#111827',
    borderLeft: '1px solid #1f2937',
    overflowY: 'auto',
    display: 'flex',
    flexDirection: 'column'
  },
  panelSection: {
    padding: '14px',
    borderBottom: '1px solid #1f2937'
  },
  sectionTitle: {
    fontSize: '10px',
    color: '#4da6ff',
    textTransform: 'uppercase',
    letterSpacing: '1.5px',
    margin: '0 0 10px 0',
    fontWeight: '700'
  },
  junctionList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '6px'
  },
  junctionItem: {
    display: 'flex',
    alignItems: 'flex-start',
    gap: '8px',
    padding: '7px',
    borderRadius: '6px',
    cursor: 'pointer',
    background: 'rgba(255,255,255,0.02)',
    border: '1px solid transparent'
  },
  junctionDot: {
    width: '9px',
    height: '9px',
    borderRadius: '50%',
    flexShrink: 0,
    marginTop: '3px'
  },
  junctionInfo: {
    display: 'flex',
    flexDirection: 'column',
    gap: '2px',
    minWidth: 0
  },
  junctionName: {
    fontSize: '12px',
    fontWeight: '600',
    color: '#e5e7eb',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap'
  },
  junctionMeta: {
    fontSize: '10px',
    color: '#6b7280'
  },
  junctionCount: {
    fontSize: '10px',
    color: '#4da6ff'
  },
  alertItem: {
    background: 'rgba(255,255,255,0.03)',
    borderRadius: '6px',
    padding: '7px 9px',
    marginBottom: '5px'
  }
};