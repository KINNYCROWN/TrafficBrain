import React, { useState, useEffect } from 'react';
import { systemAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';
import Layout from '../components/Layout';

export default function SystemHealth() {
  const { isAdmin } = useAuth();
  const [health, setHealth] = useState(null);
  const [stats, setStats] = useState(null);
  const [junctionStatus, setJunctionStatus] = useState(null);
  const [logs, setLogs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('overview');

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [healthRes, statsRes, statusRes] = await Promise.all([
          systemAPI.getHealth(),
          systemAPI.getStats(),
          systemAPI.getJunctionStatus()
        ]);
        setHealth(healthRes.data);
        setStats(statsRes.data);
        setJunctionStatus(statusRes.data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();

    const interval = setInterval(fetchData, 30000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (activeTab === 'logs' && isAdmin()) {
      systemAPI.getLogs()
        .then(res => setLogs(res.data.logs || []))
        .catch(console.error);
    }
  }, [activeTab]);

  if (loading) return (
    <Layout>
      <div style={styles.loading}>Loading system health...</div>
    </Layout>
  );

  return (
    <Layout>
      <div style={styles.pageHeader}>
        <h2 style={styles.pageTitle}>⚙️ System Health</h2>
        <div style={styles.headerRight}>
          {health && (
            <span style={{
              ...styles.healthBadge,
              background: health.status === 'Healthy'
                ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
              color: health.status === 'Healthy' ? '#22c55e' : '#ef4444',
              border: `1px solid ${health.status === 'Healthy' ? '#22c55e' : '#ef4444'}`
            }}>
              {health.status === 'Healthy' ? '● HEALTHY' : '⚠ DEGRADED'}
            </span>
          )}
          {health && (
            <span style={styles.uptime}>
              Uptime: {health.uptime}
            </span>
          )}
        </div>
      </div>

      <div style={styles.tabBar}>
        {['overview', 'junctions', ...(isAdmin() ? ['logs'] : [])].map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            style={{
              ...styles.tab,
              background: activeTab === tab ? '#2563eb' : 'transparent',
              color: activeTab === tab ? '#fff' : '#6b7280',
              borderBottom: activeTab === tab
                ? '2px solid #2563eb' : '2px solid transparent'
            }}>
            {tab.charAt(0).toUpperCase() + tab.slice(1)}
          </button>
        ))}
      </div>

      <div style={styles.content}>

        {activeTab === 'overview' && (
          <div style={styles.grid} className="card-grid three-col">

            <div style={styles.card}>
              <h3 style={styles.cardTitle}>🖥️ System Information</h3>
              {health && (
                <div style={styles.infoList}>
                  {[
                    { label: 'Status', value: health.status, color: health.status === 'Healthy' ? '#22c55e' : '#ef4444' },
                    { label: 'Database', value: health.database, color: health.database === 'Connected' ? '#22c55e' : '#ef4444' },
                    { label: 'Version', value: health.version, color: '#4da6ff' },
                    { label: 'Environment', value: health.environment, color: '#9ca3af' },
                    { label: 'Uptime', value: health.uptime, color: '#f97316' },
                  ].map((item, i) => (
                    <div key={i} style={styles.infoRow}>
                      <span style={styles.infoLabel}>{item.label}</span>
                      <span style={{ ...styles.infoValue, color: item.color }}>
                        {item.value}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {stats && (
              <div style={styles.card}>
                <h3 style={styles.cardTitle}>🗄️ Database Statistics</h3>
                <div style={styles.infoList}>
                  {[
                    { label: 'Total Users', value: stats.totalUsers },
                    { label: 'Total Junctions', value: stats.totalJunctions },
                    { label: 'Online Junctions', value: stats.onlineJunctions },
                    { label: 'Vehicle Records', value: stats.totalVehicleRecords },
                    { label: 'Total Incidents', value: stats.totalIncidents },
                    { label: 'Total Emergencies', value: stats.totalEmergencies },
                    { label: 'Total Alerts', value: stats.totalAlerts },
                    { label: 'System Logs', value: stats.totalLogs },
                  ].map((item, i) => (
                    <div key={i} style={styles.infoRow}>
                      <span style={styles.infoLabel}>{item.label}</span>
                      <span style={{ ...styles.infoValue, color: '#4da6ff' }}>
                        {item.value?.toLocaleString()}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {junctionStatus && (
              <div style={styles.card}>
                <h3 style={styles.cardTitle}>🚦 Junction Network Summary</h3>
                <div style={styles.summaryGrid}>
                  <div style={styles.summaryItem}>
                    <span style={{ ...styles.summaryValue, color: '#4da6ff' }}>
                      {junctionStatus.total}
                    </span>
                    <span style={styles.summaryLabel}>Total</span>
                  </div>
                  <div style={styles.summaryItem}>
                    <span style={{ ...styles.summaryValue, color: '#22c55e' }}>
                      {junctionStatus.online}
                    </span>
                    <span style={styles.summaryLabel}>Online</span>
                  </div>
                  <div style={styles.summaryItem}>
                    <span style={{ ...styles.summaryValue, color: '#ef4444' }}>
                      {junctionStatus.offline}
                    </span>
                    <span style={styles.summaryLabel}>Offline</span>
                  </div>
                  <div style={styles.summaryItem}>
                    <span style={{ ...styles.summaryValue, color: '#6b7280' }}>
                      {junctionStatus.neverConnected}
                    </span>
                    <span style={styles.summaryLabel}>Never Connected</span>
                  </div>
                </div>
              </div>
            )}

          </div>
        )}

        {activeTab === 'junctions' && junctionStatus && (
          <div style={styles.tableWrapper}>
            <table style={styles.table} className="responsive-table">
              <thead>
                <tr style={styles.tableHead}>
                  <th style={styles.th}>Junction</th>
                  <th style={styles.th}>District</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th}>Control Mode</th>
                  <th style={styles.th} className="hide-mobile">Last Data</th>
                  <th style={styles.th} className="hide-mobile">Minutes Ago</th>
                </tr>
              </thead>
              <tbody>
                {junctionStatus.junctions.map((j, i) => (
                  <tr key={i} style={{
                    ...styles.tableRow,
                    background: i % 2 === 0 ? 'rgba(255,255,255,0.02)' : 'transparent'
                  }}>
                    <td style={styles.td}>{j.name}</td>
                    <td style={styles.td}>{j.district}</td>
                    <td style={styles.td}>
                      <span style={{
                        ...styles.statusBadge,
                        background: j.status === 'Online'
                          ? 'rgba(34,197,94,0.15)'
                          : j.status === 'Offline'
                          ? 'rgba(239,68,68,0.15)'
                          : 'rgba(107,114,128,0.15)',
                        color: j.status === 'Online' ? '#22c55e'
                          : j.status === 'Offline' ? '#ef4444' : '#6b7280'
                      }}>
                        {j.status}
                      </span>
                    </td>
                    <td style={styles.td}>
                      <span style={{
                        ...styles.modeBadge,
                        color: j.controlMode === 'Auto' ? '#22c55e' : '#f97316'
                      }}>
                        {j.controlMode}
                      </span>
                    </td>
                    <td style={styles.td} className="hide-mobile">
                      {j.lastDataReceived
                        ? new Date(j.lastDataReceived).toLocaleString()
                        : 'Never'}
                    </td>
                    <td style={styles.td} className="hide-mobile">
                      {j.minutesSinceLastData >= 0
                        ? `${j.minutesSinceLastData} min`
                        : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {activeTab === 'logs' && isAdmin() && (
          <div style={styles.tableWrapper}>
            {logs.length === 0 ? (
              <div style={styles.empty}>No logs found</div>
            ) : (
              <table style={styles.table} className="responsive-table">
                <thead>
                  <tr style={styles.tableHead}>
                    <th style={styles.th}>Timestamp</th>
                    <th style={styles.th}>User</th>
                    <th style={styles.th}>Role</th>
                    <th style={styles.th}>Action</th>
                    <th style={styles.th} className="hide-mobile">Details</th>
                    <th style={styles.th} className="hide-mobile">IP Address</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((log, i) => (
                    <tr key={i} style={{
                      ...styles.tableRow,
                      background: i % 2 === 0 ? 'rgba(255,255,255,0.02)' : 'transparent'
                    }}>
                      <td style={styles.td}>
                        {new Date(log.timestamp).toLocaleString()}
                      </td>
                      <td style={styles.td}>{log.userName}</td>
                      <td style={styles.td}>
                        <span style={{
                          ...styles.roleBadge,
                          color: log.userRole === 'Admin' ? '#ef4444'
                            : log.userRole === 'Supervisor' ? '#f97316'
                            : '#4da6ff'
                        }}>
                          {log.userRole}
                        </span>
                      </td>
                      <td style={styles.td}>
                        <span style={styles.actionBadge}>{log.action}</span>
                      </td>
                      <td style={{ ...styles.td, maxWidth: '300px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} className="hide-mobile">
                        {log.details}
                      </td>
                      <td style={styles.td} className="hide-mobile">{log.ipAddress}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        )}

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
  healthBadge: {
    padding: '6px 14px',
    borderRadius: '20px',
    fontSize: '13px',
    fontWeight: '700',
    letterSpacing: '1px'
  },
  uptime: {
    fontSize: '13px',
    color: '#6b7280'
  },
  tabBar: {
    background: '#111827',
    borderBottom: '1px solid #1f2937',
    padding: '0 24px',
    display: 'flex',
    gap: '4px',
    overflowX: 'auto'
  },
  tab: {
    padding: '12px 20px',
    fontSize: '13px',
    fontWeight: '600',
    cursor: 'pointer',
    border: 'none',
    background: 'transparent',
    textTransform: 'capitalize',
    whiteSpace: 'nowrap'
  },
  content: {
    padding: '20px'
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(3, 1fr)',
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
  infoList: {
    display: 'flex',
    flexDirection: 'column',
    gap: '10px'
  },
  infoRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '6px 0',
    borderBottom: '1px solid #1f2937'
  },
  infoLabel: {
    fontSize: '13px',
    color: '#6b7280'
  },
  infoValue: {
    fontSize: '13px',
    fontWeight: '600'
  },
  summaryGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: '12px'
  },
  summaryItem: {
    background: 'rgba(255,255,255,0.03)',
    borderRadius: '8px',
    padding: '12px',
    textAlign: 'center'
  },
  summaryValue: {
    display: 'block',
    fontSize: '28px',
    fontWeight: '800'
  },
  summaryLabel: {
    display: 'block',
    fontSize: '11px',
    color: '#6b7280',
    marginTop: '4px',
    textTransform: 'uppercase'
  },
  tableWrapper: {
    overflowX: 'auto',
    background: '#111827',
    borderRadius: '10px',
    border: '1px solid #1f2937'
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse'
  },
  tableHead: {
    background: '#1f2937'
  },
  th: {
    padding: '12px 16px',
    textAlign: 'left',
    fontSize: '11px',
    color: '#6b7280',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    borderBottom: '1px solid #374151',
    whiteSpace: 'nowrap'
  },
  tableRow: {
    borderBottom: '1px solid #1f2937'
  },
  td: {
    padding: '12px 16px',
    fontSize: '13px',
    color: '#d1d5db'
  },
  statusBadge: {
    padding: '2px 8px',
    borderRadius: '4px',
    fontSize: '11px',
    fontWeight: '700'
  },
  modeBadge: {
    fontSize: '12px',
    fontWeight: '600'
  },
  roleBadge: {
    fontSize: '12px',
    fontWeight: '700'
  },
  actionBadge: {
    background: 'rgba(77,166,255,0.1)',
    color: '#4da6ff',
    padding: '2px 8px',
    borderRadius: '4px',
    fontSize: '11px',
    fontFamily: 'monospace'
  },
  empty: {
    textAlign: 'center',
    padding: '40px',
    color: '#6b7280',
    fontSize: '14px'
  }
};
