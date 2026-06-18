import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { LineChart, Line, BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts';
import { junctionsAPI, trafficAPI, analyticsAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';
import signalRService from '../services/signalr';
import Layout from '../components/Layout';

export default function JunctionDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, canControl } = useAuth();

  const [junction, setJunction] = useState(null);
  const [liveData, setLiveData] = useState(null);
  const [history, setHistory] = useState([]);
  const [performance, setPerformance] = useState(null);
  const [overrideHistory, setOverrideHistory] = useState([]);
  const [loading, setLoading] = useState(true);

  const [overrideMode, setOverrideMode] = useState('');
  const [overrideReason, setOverrideReason] = useState('');
  const [overrideDirection, setOverrideDirection] = useState('');
  const [overrideDuration, setOverrideDuration] = useState(60);
  const [overrideMsg, setOverrideMsg] = useState('');

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [jRes, lRes, hRes, pRes, oRes] = await Promise.all([
          junctionsAPI.getById(id),
          trafficAPI.getLiveData(id),
          trafficAPI.getHistoricalCounts(id, 1),
          analyticsAPI.getPerformance(id),
          trafficAPI.getOverrideHistory(id)
        ]);
        setJunction(jRes.data);
        setLiveData(lRes.data);
        setHistory(hRes.data.slice(0, 30).reverse());
        setPerformance(pRes.data);
        setOverrideHistory(oRes.data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();

    const handleVehicleUpdate = (data) => {
      if (data.junctionId === parseInt(id)) {
        setLiveData(data);
        setHistory(prev => [...prev.slice(-29), {
          ...data,
          time: new Date().toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })
        }]);
      }
    };

    signalRService.on('VehicleCountUpdated', handleVehicleUpdate);

    return () => {
      signalRService.off('VehicleCountUpdated', handleVehicleUpdate);
    };
  }, [id]);

  const handleOverride = async () => {
    if (!overrideMode || !overrideReason) {
      setOverrideMsg('Please fill in mode and reason');
      return;
    }
    try {
      await trafficAPI.overrideSignal({
        junctionId: parseInt(id),
        mode: overrideMode,
        reason: overrideReason,
        forcedDirection: overrideDirection,
        customGreenDuration: overrideDuration
      });
      setOverrideMsg(`✅ Junction set to ${overrideMode} mode`);
      setJunction(prev => ({ ...prev, controlMode: overrideMode }));
    } catch (err) {
      setOverrideMsg('❌ Failed to override — check permissions');
    }
  };

  const handleReturnToAuto = async () => {
    try {
      await trafficAPI.returnToAuto(parseInt(id));
      setOverrideMsg('✅ Junction returned to AUTO mode');
      setJunction(prev => ({ ...prev, controlMode: 'Auto' }));
    } catch {
      setOverrideMsg('❌ Failed to return to auto');
    }
  };

  if (loading) return (
    <Layout>
      <div style={styles.loading}>Loading junction data...</div>
    </Layout>
  );

  if (!junction) return (
    <Layout>
      <div style={styles.loading}>Junction not found</div>
    </Layout>
  );

  const congestionScore = liveData?.congestionScore ?? 0;
  const congestionColor =
    congestionScore >= 75 ? '#ef4444' :
    congestionScore >= 50 ? '#f97316' :
    congestionScore >= 25 ? '#eab308' : '#22c55e';

  const chartData = history.map((h, i) => ({
    time: h.time || `T-${history.length - i}`,
    total: h.totalVehicles || 0,
    cars: h.cars || 0,
    bikes: h.motorcycles || 0,
    buses: h.buses || 0,
    trucks: h.trucks || 0,
    congestion: h.congestionScore || 0
  }));

  return (
    <Layout>
      <div style={styles.pageHeader}>
        <div style={styles.headerLeft}>
          <button onClick={() => navigate('/')} style={styles.backBtn}>← Back</button>
          <div style={{ minWidth: 0 }}>
            <h2 style={styles.title}>{junction.name}</h2>
            <p style={styles.subtitle}>
              {junction.district} • {junction.roadNames}
            </p>
          </div>
        </div>
        <div style={styles.headerRight}>
          <span style={{
            ...styles.modeBadge,
            background: junction.controlMode === 'Auto'
              ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
            color: junction.controlMode === 'Auto' ? '#22c55e' : '#ef4444',
            border: `1px solid ${junction.controlMode === 'Auto' ? '#22c55e' : '#ef4444'}`
          }}>
            {junction.controlMode === 'Auto' ? '🤖 AUTO' : '👤 MANUAL'}
          </span>
          <span style={{
            ...styles.onlineBadge,
            color: junction.isOnline ? '#22c55e' : '#ef4444'
          }}>
            {junction.isOnline ? '● Online' : '○ Offline'}
          </span>
        </div>
      </div>

      <div style={styles.content} className="junction-detail-content">

        <div style={styles.leftCol}>

          <div style={styles.card}>
            <h3 style={styles.cardTitle}>🔴 Live Vehicle Count</h3>
            <div style={styles.countGrid} className="count-grid">
              {[
                { label: 'Cars', value: liveData?.cars ?? 0, color: '#22c55e' },
                { label: 'Motorcycles', value: liveData?.motorcycles ?? 0, color: '#f97316' },
                { label: 'Buses', value: liveData?.buses ?? 0, color: '#3b82f6' },
                { label: 'Trucks', value: liveData?.trucks ?? 0, color: '#8b5cf6' },
                { label: 'Pedestrians', value: liveData?.pedestrians ?? 0, color: '#eab308' },
                { label: 'TOTAL', value: liveData?.totalVehicles ?? 0, color: '#4da6ff' },
              ].map((item, i) => (
                <div key={i} style={styles.countItem}>
                  <span style={{ ...styles.countNum, color: item.color }}>
                    {item.value}
                  </span>
                  <span style={styles.countLabel}>{item.label}</span>
                </div>
              ))}
            </div>

            <div style={styles.meterSection}>
              <div style={styles.meterLabel}>
                <span>Congestion Score</span>
                <span style={{ color: congestionColor, fontWeight: '700' }}>
                  {congestionScore}/100
                </span>
              </div>
              <div style={styles.meterBg}>
                <div style={{
                  ...styles.meterFill,
                  width: `${congestionScore}%`,
                  background: congestionColor
                }} />
              </div>
              <div style={{ textAlign: 'right', marginTop: '4px' }}>
                <span style={{
                  fontSize: '11px',
                  color: congestionColor,
                  fontWeight: '700'
                }}>
                  {congestionScore >= 75 ? 'GRIDLOCK' :
                   congestionScore >= 50 ? 'HEAVY' :
                   congestionScore >= 25 ? 'MODERATE' : 'FREE FLOW'}
                </span>
              </div>
            </div>

            {liveData?.averageSpeed > 0 && (
              <div style={styles.speedBadge}>
                🚗 Avg Speed: {liveData.averageSpeed} km/h
              </div>
            )}
          </div>

          <div style={styles.card}>
            <h3 style={styles.cardTitle}>📈 Vehicle Count — Last 30 Updates</h3>
            <ResponsiveContainer width="100%" height={180}>
              <LineChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
                <XAxis dataKey="time" tick={{ fontSize: 10, fill: '#6b7280' }} />
                <YAxis tick={{ fontSize: 10, fill: '#6b7280' }} />
                <Tooltip
                  contentStyle={{
                    background: '#111827',
                    border: '1px solid #374151',
                    borderRadius: '8px',
                    color: '#fff'
                  }}
                />
                <Line type="monotone" dataKey="total" stroke="#4da6ff" strokeWidth={2} dot={false} name="Total" />
                <Line type="monotone" dataKey="congestion" stroke="#ef4444" strokeWidth={1} dot={false} name="Congestion" strokeDasharray="4 4" />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div style={styles.card}>
            <h3 style={styles.cardTitle}>🚗 Vehicle Type Breakdown</h3>
            <ResponsiveContainer width="100%" height={160}>
              <BarChart data={chartData.slice(-10)}>
                <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
                <XAxis dataKey="time" tick={{ fontSize: 10, fill: '#6b7280' }} />
                <YAxis tick={{ fontSize: 10, fill: '#6b7280' }} />
                <Tooltip
                  contentStyle={{
                    background: '#111827',
                    border: '1px solid #374151',
                    borderRadius: '8px',
                    color: '#fff'
                  }}
                />
                <Bar dataKey="cars" fill="#22c55e" name="Cars" />
                <Bar dataKey="bikes" fill="#f97316" name="Bikes" />
                <Bar dataKey="buses" fill="#3b82f6" name="Buses" />
                <Bar dataKey="trucks" fill="#8b5cf6" name="Trucks" />
              </BarChart>
            </ResponsiveContainer>
          </div>

        </div>

        <div style={styles.rightCol} className="junction-right-col">

          {canControl() && (
            <div style={{ ...styles.card, border: '1px solid rgba(46,117,182,0.4)' }}>
              <h3 style={styles.cardTitle}>🎛️ Signal Control Panel</h3>
              <p style={styles.cardSubtitle}>
                Logged in as: {user?.fullName} ({user?.role})
              </p>

              <div style={styles.modeButtons}>
                {['Auto', 'Manual', 'Emergency'].map(mode => (
                  <button
                    key={mode}
                    onClick={() => setOverrideMode(mode)}
                    style={{
                      ...styles.modeBtn,
                      background: overrideMode === mode
                        ? mode === 'Emergency' ? '#ef4444'
                          : mode === 'Manual' ? '#f97316' : '#22c55e'
                        : 'rgba(255,255,255,0.05)',
                      border: `1px solid ${overrideMode === mode
                        ? mode === 'Emergency' ? '#ef4444'
                          : mode === 'Manual' ? '#f97316' : '#22c55e'
                        : 'rgba(255,255,255,0.1)'}`
                    }}>
                    {mode === 'Auto' ? '🤖' : mode === 'Manual' ? '👤' : '🚨'} {mode}
                  </button>
                ))}
              </div>

              {overrideMode && overrideMode !== 'Auto' && (
                <>
                  <div style={styles.formGroup}>
                    <label style={styles.formLabel}>Direction to Force Green</label>
                    <select
                      value={overrideDirection}
                      onChange={e => setOverrideDirection(e.target.value)}
                      style={styles.select}>
                      <option value="">All Directions</option>
                      <option value="North">North</option>
                      <option value="South">South</option>
                      <option value="East">East</option>
                      <option value="West">West</option>
                    </select>
                  </div>

                  <div style={styles.formGroup}>
                    <label style={styles.formLabel}>Green Duration (seconds)</label>
                    <input
                      type="number"
                      value={overrideDuration}
                      onChange={e => setOverrideDuration(parseInt(e.target.value))}
                      style={styles.input}
                      min="10"
                      max="300"
                    />
                  </div>

                  <div style={styles.formGroup}>
                    <label style={styles.formLabel}>Reason (Required)</label>
                    <input
                      type="text"
                      value={overrideReason}
                      onChange={e => setOverrideReason(e.target.value)}
                      placeholder="e.g. Accident at junction, VIP convoy..."
                      style={styles.input}
                    />
                  </div>
                </>
              )}

              {overrideMode === 'Auto' && (
                <div style={styles.formGroup}>
                  <label style={styles.formLabel}>Reason</label>
                  <input
                    type="text"
                    value={overrideReason}
                    onChange={e => setOverrideReason(e.target.value)}
                    placeholder="Returning to auto control..."
                    style={styles.input}
                  />
                </div>
              )}

              <div style={styles.controlButtons}>
                <button onClick={handleOverride} style={styles.applyBtn}>
                  Apply Override
                </button>
                <button onClick={handleReturnToAuto} style={styles.autoBtn}>
                  Return to AUTO
                </button>
              </div>

              {overrideMsg && (
                <div style={{
                  ...styles.overrideMsg,
                  color: overrideMsg.startsWith('✅') ? '#22c55e' : '#ef4444'
                }}>
                  {overrideMsg}
                </div>
              )}
            </div>
          )}

          {performance?.stats && (
            <div style={styles.card}>
              <h3 style={styles.cardTitle}>📊 Performance — Last 24h</h3>
              <div style={styles.perfGrid}>
                <div style={styles.perfItem}>
                  <span style={styles.perfValue}>
                    {performance.stats.totalVehiclesLast24h}
                  </span>
                  <span style={styles.perfLabel}>Total Vehicles</span>
                </div>
                <div style={styles.perfItem}>
                  <span style={styles.perfValue}>
                    {performance.stats.averageCongestionScore}
                  </span>
                  <span style={styles.perfLabel}>Avg Congestion</span>
                </div>
                <div style={styles.perfItem}>
                  <span style={styles.perfValue}>
                    {performance.stats.peakVehicleCount}
                  </span>
                  <span style={styles.perfLabel}>Peak Vehicles</span>
                </div>
                <div style={styles.perfItem}>
                  <span style={styles.perfValue}>
                    {performance.stats.dataPoints}
                  </span>
                  <span style={styles.perfLabel}>Data Points</span>
                </div>
              </div>
            </div>
          )}

          {overrideHistory.length > 0 && (
            <div style={styles.card}>
              <h3 style={styles.cardTitle}>📋 Override History</h3>
              {overrideHistory.slice(0, 5).map((o, i) => (
                <div key={i} style={styles.overrideItem}>
                  <div style={styles.overrideHeader}>
                    <span style={{
                      ...styles.overrideBadge,
                      background: o.mode === 'Auto'
                        ? 'rgba(34,197,94,0.2)' : 'rgba(239,68,68,0.2)',
                      color: o.mode === 'Auto' ? '#22c55e' : '#ef4444'
                    }}>
                      {o.mode}
                    </span>
                    <span style={styles.overrideTime}>
                      {new Date(o.startTime).toLocaleString()}
                    </span>
                  </div>
                  <p style={styles.overrideReason}>{o.reason}</p>
                  <p style={styles.overrideOfficer}>
                    By: {o.officerName} ({o.officerBadge})
                  </p>
                </div>
              ))}
            </div>
          )}

        </div>
      </div>
    </Layout>
  );
}

const styles = {
  loading: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: 'calc(100vh - 52px)',
    color: '#4da6ff',
    fontSize: '18px'
  },
  pageHeader: {
    background: '#111827',
    borderBottom: '1px solid #1f2937',
    padding: '16px 24px',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: '12px'
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '16px',
    minWidth: 0,
    flex: 1
  },
  backBtn: {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(255,255,255,0.1)',
    borderRadius: '6px',
    padding: '8px 16px',
    color: '#9ca3af',
    cursor: 'pointer',
    fontSize: '14px',
    flexShrink: 0
  },
  title: {
    margin: 0,
    fontSize: '18px',
    color: '#f9fafb',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap'
  },
  subtitle: {
    margin: '2px 0 0 0',
    fontSize: '13px',
    color: '#6b7280',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap'
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flexShrink: 0
  },
  modeBadge: {
    padding: '6px 14px',
    borderRadius: '20px',
    fontSize: '13px',
    fontWeight: '700',
    letterSpacing: '1px'
  },
  onlineBadge: {
    fontSize: '13px',
    fontWeight: '600'
  },
  content: {
    display: 'flex',
    gap: '20px',
    padding: '20px',
    alignItems: 'flex-start',
    flexWrap: 'wrap'
  },
  leftCol: {
    flex: 1,
    minWidth: '300px',
    display: 'flex',
    flexDirection: 'column',
    gap: '16px'
  },
  rightCol: {
    width: '360px',
    display: 'flex',
    flexDirection: 'column',
    gap: '16px'
  },
  card: {
    background: '#111827',
    border: '1px solid #1f2937',
    borderRadius: '10px',
    padding: '16px'
  },
  cardTitle: {
    margin: '0 0 12px 0',
    fontSize: '13px',
    color: '#4da6ff',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    fontWeight: '700'
  },
  cardSubtitle: {
    margin: '-8px 0 12px 0',
    fontSize: '12px',
    color: '#6b7280'
  },
  countGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(3, 1fr)',
    gap: '10px',
    marginBottom: '16px'
  },
  countItem: {
    background: 'rgba(255,255,255,0.03)',
    borderRadius: '8px',
    padding: '10px',
    textAlign: 'center'
  },
  countNum: {
    display: 'block',
    fontSize: '28px',
    fontWeight: '800',
    lineHeight: 1
  },
  countLabel: {
    display: 'block',
    fontSize: '11px',
    color: '#6b7280',
    marginTop: '4px',
    textTransform: 'uppercase'
  },
  meterSection: {
    marginTop: '8px'
  },
  meterLabel: {
    display: 'flex',
    justifyContent: 'space-between',
    fontSize: '13px',
    color: '#9ca3af',
    marginBottom: '6px'
  },
  meterBg: {
    background: 'rgba(255,255,255,0.05)',
    borderRadius: '4px',
    height: '8px',
    overflow: 'hidden'
  },
  meterFill: {
    height: '100%',
    borderRadius: '4px',
    transition: 'width 0.5s ease, background 0.5s ease'
  },
  speedBadge: {
    marginTop: '12px',
    background: 'rgba(77,166,255,0.1)',
    border: '1px solid rgba(77,166,255,0.2)',
    borderRadius: '6px',
    padding: '6px 12px',
    fontSize: '13px',
    color: '#4da6ff',
    display: 'inline-block'
  },
  modeButtons: {
    display: 'flex',
    gap: '8px',
    marginBottom: '16px',
    flexWrap: 'wrap'
  },
  modeBtn: {
    flex: 1,
    minWidth: '90px',
    padding: '10px',
    borderRadius: '6px',
    color: '#ffffff',
    fontSize: '12px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  formGroup: {
    marginBottom: '12px'
  },
  formLabel: {
    display: 'block',
    fontSize: '11px',
    color: '#9ca3af',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    marginBottom: '6px'
  },
  input: {
    width: '100%',
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(46,117,182,0.3)',
    borderRadius: '6px',
    padding: '8px 12px',
    color: '#ffffff',
    fontSize: '13px',
    boxSizing: 'border-box'
  },
  select: {
    width: '100%',
    background: '#1f2937',
    border: '1px solid rgba(46,117,182,0.3)',
    borderRadius: '6px',
    padding: '8px 12px',
    color: '#ffffff',
    fontSize: '13px'
  },
  controlButtons: {
    display: 'flex',
    gap: '8px',
    marginTop: '8px',
    flexWrap: 'wrap'
  },
  applyBtn: {
    flex: 1,
    minWidth: '120px',
    background: '#2563eb',
    border: 'none',
    borderRadius: '6px',
    padding: '10px',
    color: '#ffffff',
    fontSize: '13px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  autoBtn: {
    flex: 1,
    minWidth: '120px',
    background: 'rgba(34,197,94,0.15)',
    border: '1px solid #22c55e',
    borderRadius: '6px',
    padding: '10px',
    color: '#22c55e',
    fontSize: '13px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  overrideMsg: {
    marginTop: '10px',
    fontSize: '13px',
    textAlign: 'center',
    padding: '8px',
    borderRadius: '6px',
    background: 'rgba(255,255,255,0.03)'
  },
  perfGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: '10px'
  },
  perfItem: {
    background: 'rgba(255,255,255,0.03)',
    borderRadius: '8px',
    padding: '10px',
    textAlign: 'center'
  },
  perfValue: {
    display: 'block',
    fontSize: '22px',
    fontWeight: '800',
    color: '#4da6ff'
  },
  perfLabel: {
    display: 'block',
    fontSize: '11px',
    color: '#6b7280',
    marginTop: '4px'
  },
  overrideItem: {
    background: 'rgba(255,255,255,0.02)',
    borderRadius: '6px',
    padding: '10px',
    marginBottom: '8px',
    border: '1px solid #1f2937'
  },
  overrideHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '6px',
    flexWrap: 'wrap',
    gap: '6px'
  },
  overrideBadge: {
    padding: '2px 8px',
    borderRadius: '4px',
    fontSize: '11px',
    fontWeight: '700'
  },
  overrideTime: {
    fontSize: '11px',
    color: '#6b7280'
  },
  overrideReason: {
    margin: '0 0 4px 0',
    fontSize: '12px',
    color: '#e5e7eb'
  },
  overrideOfficer: {
    margin: 0,
    fontSize: '11px',
    color: '#6b7280'
  }
};
