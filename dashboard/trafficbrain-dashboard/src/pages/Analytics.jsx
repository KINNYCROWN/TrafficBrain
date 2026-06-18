import React, { useState, useEffect } from 'react';
import { BarChart, Bar, PieChart, Pie, Cell, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Legend } from 'recharts';
import { analyticsAPI, junctionsAPI } from '../services/api';
import Layout from '../components/Layout';

const COLORS = ['#4da6ff', '#22c55e', '#f97316', '#8b5cf6', '#eab308'];

export default function Analytics() {
  const [national, setNational] = useState(null);
  const [junctions, setJunctions] = useState([]);
  const [selectedJunction, setSelectedJunction] = useState(1);
  const [performance, setPerformance] = useState(null);
  const [peakHours, setPeakHours] = useState(null);
  const [comparison, setComparison] = useState(null);
  const [environment, setEnvironment] = useState(null);
  const [breakdown, setBreakdown] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [natRes, jRes, envRes, breakRes] = await Promise.all([
          analyticsAPI.getNational(),
          junctionsAPI.getAll(),
          analyticsAPI.getEnvironment(),
          analyticsAPI.getVehicleBreakdown(24)
        ]);
        setNational(natRes.data);
        setJunctions(jRes.data);
        setEnvironment(envRes.data);
        setBreakdown(breakRes.data);
        if (jRes.data.length > 0) setSelectedJunction(jRes.data[0].id);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  useEffect(() => {
    if (!selectedJunction) return;
    const fetchJunctionData = async () => {
      try {
        const [perfRes, peakRes, compRes] = await Promise.all([
          analyticsAPI.getPerformance(selectedJunction),
          analyticsAPI.getPeakHours(selectedJunction),
          analyticsAPI.getComparison(selectedJunction)
        ]);
        setPerformance(perfRes.data);
        setPeakHours(peakRes.data);
        setComparison(compRes.data);
      } catch (err) {
        console.error(err);
      }
    };
    fetchJunctionData();
  }, [selectedJunction]);

  const handleExport = async () => {
    try {
      const res = await analyticsAPI.exportData(selectedJunction, 7);
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `TrafficBrain_Junction${selectedJunction}_Export.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err) {
      console.error('Export failed:', err);
    }
  };

  const pieData = breakdown ? [
    { name: 'Cars', value: breakdown.totalCars },
    { name: 'Motorcycles', value: breakdown.totalMotorcycles },
    { name: 'Buses', value: breakdown.totalBuses },
    { name: 'Trucks', value: breakdown.totalTrucks },
    { name: 'Pedestrians', value: breakdown.totalPedestrians },
  ] : [];

  const peakData = peakHours?.hourlyBreakdown?.map(h => ({
    hour: `${h.hour}:00`,
    vehicles: Math.round(h.averageVehicles),
    congestion: Math.round(h.averageCongestion)
  })) || [];

  if (loading) return (
    <Layout>
      <div style={styles.loading}>Loading analytics...</div>
    </Layout>
  );

  return (
    <Layout>
      <div style={styles.pageHeader}>
        <h2 style={styles.pageTitle}>📊 Traffic Analytics</h2>
        <div style={styles.headerRight}>
          <select
            value={selectedJunction}
            onChange={e => setSelectedJunction(parseInt(e.target.value))}
            style={styles.select}>
            {junctions.map(j => (
              <option key={j.id} value={j.id}>{j.name}</option>
            ))}
          </select>
          <button onClick={handleExport} style={styles.exportBtn}>
            ⬇️ Export CSV
          </button>
        </div>
      </div>

      <div style={styles.content}>

        {national && (
          <div style={styles.statsRow}>
            {[
              { label: 'Total Junctions', value: national.totalJunctions, color: '#4da6ff' },
              { label: 'Online Now', value: national.onlineJunctions, color: '#22c55e' },
              { label: 'Vehicles Today', value: national.totalVehiclesToday, color: '#f97316' },
              { label: 'Active Incidents', value: national.activeIncidents, color: '#eab308' },
              { label: 'CO2 Saved (kg)', value: Math.round(national.totalCO2SavedKg), color: '#a78bfa' },
              { label: 'Districts', value: national.totalDistricts, color: '#4da6ff' },
            ].map((s, i) => (
              <div key={i} style={styles.statCard}>
                <span style={{ ...styles.statValue, color: s.color }}>{s.value ?? 0}</span>
                <span style={styles.statLabel}>{s.label}</span>
              </div>
            ))}
          </div>
        )}

        <div style={styles.grid} className="card-grid">

          <div style={styles.card}>
            <h3 style={styles.cardTitle}>🤖 AI vs Fixed Timer Comparison</h3>
            {comparison ? (
              <>
                <div style={styles.compGrid}>
                  <div style={styles.compItem}>
                    <span style={styles.compLabel}>TRAFFICBRAIN (AI)</span>
                    <span style={{ ...styles.compValue, color: '#22c55e' }}>
                      {comparison.adaptive?.averageWaitTime}s
                    </span>
                    <span style={styles.compSub}>Avg Wait Time</span>
                  </div>
                  <div style={styles.compVs}>VS</div>
                  <div style={styles.compItem}>
                    <span style={styles.compLabel}>Fixed Timer</span>
                    <span style={{ ...styles.compValue, color: '#ef4444' }}>
                      {comparison.fixed?.averageWaitTime}s
                    </span>
                    <span style={styles.compSub}>Avg Wait Time</span>
                  </div>
                </div>
                {comparison.improvementPercent > 0 && (
                  <div style={styles.improvement}>
                    🎯 TRAFFICBRAIN reduces wait time by{' '}
                    <strong style={{ color: '#22c55e' }}>
                      {comparison.improvementPercent}%
                    </strong>
                  </div>
                )}
                <p style={styles.conclusion}>{comparison.conclusion}</p>
              </>
            ) : (
              <p style={styles.noData}>
                Run the performance benchmark simulation to generate comparison data.
              </p>
            )}
          </div>

          <div style={styles.card}>
            <h3 style={styles.cardTitle}>🚗 Vehicle Type Breakdown (24h)</h3>
            {breakdown && breakdown.totalVehicles > 0 ? (
              <>
                <ResponsiveContainer width="100%" height={200}>
                  <PieChart>
                    <Pie
                      data={pieData}
                      cx="50%"
                      cy="50%"
                      innerRadius={50}
                      outerRadius={80}
                      paddingAngle={3}
                      dataKey="value"
                    >
                      {pieData.map((entry, index) => (
                        <Cell key={index} fill={COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip
                      contentStyle={{
                        background: '#111827',
                        border: '1px solid #374151',
                        borderRadius: '8px',
                        color: '#fff'
                      }}
                    />
                    <Legend
                      formatter={(value) => (
                        <span style={{ color: '#9ca3af', fontSize: '12px' }}>{value}</span>
                      )}
                    />
                  </PieChart>
                </ResponsiveContainer>
                <div style={styles.totalVehicles}>
                  Total: <strong style={{ color: '#4da6ff' }}>
                    {breakdown.totalVehicles}
                  </strong> vehicles
                </div>
              </>
            ) : (
              <p style={styles.noData}>No vehicle data recorded yet. Run the detector to collect data.</p>
            )}
          </div>

          <div style={{ ...styles.card, gridColumn: 'span 2' }} className="span-2">
            <h3 style={styles.cardTitle}>⏰ Peak Hours Analysis — Last 7 Days</h3>
            {peakData.length > 0 ? (
              <ResponsiveContainer width="100%" height={200}>
                <BarChart data={peakData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
                  <XAxis dataKey="hour" tick={{ fontSize: 11, fill: '#6b7280' }} />
                  <YAxis tick={{ fontSize: 11, fill: '#6b7280' }} />
                  <Tooltip
                    contentStyle={{
                      background: '#111827',
                      border: '1px solid #374151',
                      borderRadius: '8px',
                      color: '#fff'
                    }}
                  />
                  <Legend
                    formatter={(value) => (
                      <span style={{ color: '#9ca3af', fontSize: '12px' }}>{value}</span>
                    )}
                  />
                  <Bar dataKey="vehicles" fill="#4da6ff" name="Avg Vehicles" />
                  <Bar dataKey="congestion" fill="#ef4444" name="Avg Congestion" />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <p style={styles.noData}>No peak hour data yet. Data accumulates over time.</p>
            )}
            {peakHours?.peakHour !== undefined && (
              <p style={styles.peakNote}>
                🔴 Peak hour: <strong style={{ color: '#ef4444' }}>
                  {peakHours.peakHour}:00 — {peakHours.peakHour + 1}:00
                </strong>
              </p>
            )}
          </div>

          {environment && (
            <div style={{ ...styles.card, gridColumn: 'span 2' }} className="span-2">
              <h3 style={styles.cardTitle}>🌿 Environmental Impact</h3>
              <div style={styles.envGrid}>
                {[
                  { label: 'CO2 Saved', value: `${environment.totalCO2SavedKg} kg`, color: '#22c55e', icon: '🌱' },
                  { label: 'CO2 in Tonnes', value: `${environment.totalCO2SavedTonnes} t`, color: '#22c55e', icon: '🌍' },
                  { label: 'Fuel Saved', value: `${environment.totalFuelSavedLitres} L`, color: '#f97316', icon: '⛽' },
                  { label: 'Vehicles Managed', value: environment.totalVehiclesManaged, color: '#4da6ff', icon: '🚗' },
                  { label: 'Equivalent Trees', value: `${environment.equivalentTreesPlanted} 🌳`, color: '#22c55e', icon: '🌳' },
                ].map((item, i) => (
                  <div key={i} style={styles.envItem}>
                    <span style={styles.envIcon}>{item.icon}</span>
                    <span style={{ ...styles.envValue, color: item.color }}>{item.value}</span>
                    <span style={styles.envLabel}>{item.label}</span>
                  </div>
                ))}
              </div>
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
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '16px 24px',
    background: '#111827',
    borderBottom: '1px solid #1f2937',
    flexWrap: 'wrap',
    gap: '12px'
  },
  pageTitle: {
    margin: 0,
    fontSize: '18px',
    color: '#f9fafb'
  },
  headerRight: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    flexWrap: 'wrap'
  },
  select: {
    background: '#1f2937',
    border: '1px solid #374151',
    borderRadius: '6px',
    padding: '8px 12px',
    color: '#ffffff',
    fontSize: '13px'
  },
  exportBtn: {
    background: '#2563eb',
    border: 'none',
    borderRadius: '6px',
    padding: '8px 16px',
    color: '#ffffff',
    fontSize: '13px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  content: {
    padding: '20px'
  },
  statsRow: {
    display: 'flex',
    gap: '12px',
    marginBottom: '20px',
    flexWrap: 'wrap'
  },
  statCard: {
    flex: 1,
    minWidth: '120px',
    background: '#111827',
    border: '1px solid #1f2937',
    borderRadius: '10px',
    padding: '16px',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '4px'
  },
  statValue: {
    fontSize: '28px',
    fontWeight: '800'
  },
  statLabel: {
    fontSize: '11px',
    color: '#6b7280',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    textAlign: 'center'
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: '16px'
  },
  card: {
    background: '#111827',
    border: '1px solid #1f2937',
    borderRadius: '10px',
    padding: '20px'
  },
  cardTitle: {
    margin: '0 0 16px 0',
    fontSize: '13px',
    color: '#4da6ff',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    fontWeight: '700'
  },
  compGrid: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-around',
    marginBottom: '16px',
    flexWrap: 'wrap',
    gap: '12px'
  },
  compItem: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '4px'
  },
  compLabel: {
    fontSize: '12px',
    color: '#9ca3af',
    textTransform: 'uppercase',
    letterSpacing: '1px'
  },
  compValue: {
    fontSize: '36px',
    fontWeight: '800'
  },
  compSub: {
    fontSize: '11px',
    color: '#6b7280'
  },
  compVs: {
    fontSize: '20px',
    fontWeight: '800',
    color: '#374151'
  },
  improvement: {
    background: 'rgba(34,197,94,0.1)',
    border: '1px solid rgba(34,197,94,0.2)',
    borderRadius: '8px',
    padding: '10px',
    fontSize: '14px',
    color: '#9ca3af',
    textAlign: 'center',
    marginBottom: '8px'
  },
  conclusion: {
    fontSize: '12px',
    color: '#6b7280',
    textAlign: 'center',
    margin: 0
  },
  noData: {
    fontSize: '13px',
    color: '#6b7280',
    textAlign: 'center',
    padding: '20px 0'
  },
  totalVehicles: {
    textAlign: 'center',
    fontSize: '13px',
    color: '#9ca3af',
    marginTop: '8px'
  },
  peakNote: {
    marginTop: '12px',
    fontSize: '13px',
    color: '#9ca3af',
    textAlign: 'center'
  },
  envGrid: {
    display: 'flex',
    gap: '16px',
    flexWrap: 'wrap',
    justifyContent: 'space-around'
  },
  envItem: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    gap: '4px',
    minWidth: '120px'
  },
  envIcon: {
    fontSize: '28px'
  },
  envValue: {
    fontSize: '20px',
    fontWeight: '800'
  },
  envLabel: {
    fontSize: '11px',
    color: '#6b7280',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    textAlign: 'center'
  }
};
