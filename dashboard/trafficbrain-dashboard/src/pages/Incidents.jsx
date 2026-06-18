import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import Layout from '../components/Layout';
import { incidentsAPI, junctionsAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';
import signalRService from '../services/signalr';

const getSeverityColor = (severity) => {
  switch (severity) {
    case 'Critical': return '#ef4444';
    case 'High': return '#f97316';
    case 'Medium': return '#eab308';
    default: return '#22c55e';
  }
};

export default function Incidents() {
  const navigate = useNavigate();
  const { canControl } = useAuth();
  const [activeIncidents, setActiveIncidents] = useState([]);
  const [history, setHistory] = useState([]);
  const [alerts, setAlerts] = useState([]);
  const [junctions, setJunctions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('active');

  // Report form
  const [formJunction, setFormJunction] = useState('');
  const [formType, setFormType] = useState('');
  const [formDesc, setFormDesc] = useState('');
  const [formSeverity, setFormSeverity] = useState('Medium');
  const [formMsg, setFormMsg] = useState('');

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [activeRes, histRes, alertRes, jRes] = await Promise.all([
          incidentsAPI.getActive(),
          incidentsAPI.getHistory('', 7),
          incidentsAPI.getAlerts(),
          junctionsAPI.getAll()
        ]);
        setActiveIncidents(activeRes.data);
        setHistory(histRes.data);
        setAlerts(alertRes.data);
        setJunctions(jRes.data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();

    // Real-time updates
    signalRService.on('IncidentDetected', (data) => {
      setActiveIncidents(prev => [data, ...prev]);
    });

    signalRService.on('IncidentResolved', (data) => {
      setActiveIncidents(prev =>
        prev.filter(i => i.id !== data.incidentId));
    });

    signalRService.on('AlertReceived', (data) => {
      setAlerts(prev => [data, ...prev].slice(0, 50));
    });
  }, []);

  const handleResolve = async (id) => {
    try {
      await incidentsAPI.resolve(id);
      setActiveIncidents(prev => prev.filter(i => i.id !== id));
    } catch (err) {
      console.error(err);
    }
  };

  const handleAcknowledge = async (id) => {
    try {
      await incidentsAPI.acknowledgeAlert(id);
      setAlerts(prev => prev.map(a =>
        a.id === id ? { ...a, isAcknowledged: true } : a));
    } catch (err) {
      console.error(err);
    }
  };

  const handleReport = async (e) => {
    e.preventDefault();
    if (!formJunction || !formType || !formDesc) {
      setFormMsg('Please fill in all fields');
      return;
    }
    try {
      await incidentsAPI.report({
        junctionId: parseInt(formJunction),
        type: formType,
        description: formDesc,
        severity: formSeverity
      });
      setFormMsg('✅ Incident reported successfully');
      setFormJunction('');
      setFormType('');
      setFormDesc('');
    } catch {
      setFormMsg('❌ Failed to report incident');
    }
  };

  if (loading) return (
    <div style={styles.loading}>Loading incidents...</div>
  );

  return (
    <Layout>

     return (
  <Layout>
    <div style={styles.pageHeader}>
      <h2 style={styles.pageTitle}>⚠️ Incidents & Alerts</h2>
      <div style={styles.badges}>
        <span style={{ ...styles.badge, background: 'rgba(239,68,68,0.15)', color: '#ef4444', border: '1px solid #ef4444' }}>
          {activeIncidents.length} Active
        </span>
        <span style={{ ...styles.badge, background: 'rgba(234,179,8,0.15)', color: '#eab308', border: '1px solid #eab308' }}>
          {alerts.filter(a => !a.isAcknowledged).length} Unacknowledged
        </span>
      </div>
    </div>
    </Layout>

    <div style={styles.content}>

        {/* LEFT — Incidents */}
        <div style={styles.leftCol}>

          {/* Tabs */}
          <div style={styles.tabs}>
            {['active', 'history', 'alerts'].map(tab => (
              <button
                key={tab}
                onClick={() => setActiveTab(tab)}
                style={{
                  ...styles.tab,
                  background: activeTab === tab ? '#2563eb' : 'transparent',
                  color: activeTab === tab ? '#fff' : '#6b7280',
                  border: activeTab === tab ? '1px solid #2563eb' : '1px solid transparent'
                }}>
                {tab === 'active' ? `Active (${activeIncidents.length})` :
                 tab === 'history' ? `History (${history.length})` :
                 `Alerts (${alerts.length})`}
              </button>
            ))}
          </div>

          {/* Active Incidents */}
          {activeTab === 'active' && (
            <div style={styles.list}>
              {activeIncidents.length === 0 ? (
                <div style={styles.empty}>✅ No active incidents</div>
              ) : activeIncidents.map((inc, i) => (
                <div key={i} style={{
                  ...styles.incidentCard,
                  borderLeft: `4px solid ${getSeverityColor(inc.severity)}`
                }}>
                  <div style={styles.incidentHeader}>
                    <div>
                      <span style={styles.incidentType}>{inc.type}</span>
                      <span style={{
                        ...styles.severityBadge,
                        background: `${getSeverityColor(inc.severity)}20`,
                        color: getSeverityColor(inc.severity)
                      }}>
                        {inc.severity}
                      </span>
                    </div>
                    {canControl() && (
                      <button
                        onClick={() => handleResolve(inc.id)}
                        style={styles.resolveBtn}>
                        ✓ Resolve
                      </button>
                    )}
                  </div>
                  <p style={styles.incidentDesc}>{inc.description}</p>
                  <div style={styles.incidentMeta}>
                    <span>📍 {inc.junctionName}</span>
                    <span>🗺️ {inc.district}</span>
                    <span>🕐 {new Date(inc.detectedAt).toLocaleString()}</span>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* History */}
          {activeTab === 'history' && (
            <div style={styles.list}>
              {history.length === 0 ? (
                <div style={styles.empty}>No incident history</div>
              ) : history.map((inc, i) => (
                <div key={i} style={{
                  ...styles.incidentCard,
                  borderLeft: `4px solid ${inc.isResolved ? '#22c55e' : getSeverityColor(inc.severity)}`,
                  opacity: inc.isResolved ? 0.7 : 1
                }}>
                  <div style={styles.incidentHeader}>
                    <div>
                      <span style={styles.incidentType}>{inc.type}</span>
                      <span style={{
                        ...styles.severityBadge,
                        background: inc.isResolved ? 'rgba(34,197,94,0.15)' : `${getSeverityColor(inc.severity)}20`,
                        color: inc.isResolved ? '#22c55e' : getSeverityColor(inc.severity)
                      }}>
                        {inc.isResolved ? '✓ Resolved' : inc.severity}
                      </span>
                    </div>
                  </div>
                  <p style={styles.incidentDesc}>{inc.description}</p>
                  <div style={styles.incidentMeta}>
                    <span>📍 {inc.junctionName}</span>
                    <span>🕐 {new Date(inc.detectedAt).toLocaleString()}</span>
                    {inc.resolvedBy && <span>✓ {inc.resolvedBy}</span>}
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Alerts */}
          {activeTab === 'alerts' && (
            <div style={styles.list}>
              {alerts.length === 0 ? (
                <div style={styles.empty}>No alerts</div>
              ) : alerts.map((alert, i) => (
                <div key={i} style={{
                  ...styles.incidentCard,
                  borderLeft: `4px solid ${getSeverityColor(alert.severity)}`,
                  opacity: alert.isAcknowledged ? 0.5 : 1
                }}>
                  <div style={styles.incidentHeader}>
                    <div>
                      <span style={styles.incidentType}>{alert.alertType}</span>
                      <span style={{
                        ...styles.severityBadge,
                        background: alert.isAcknowledged
                          ? 'rgba(107,114,128,0.2)' : `${getSeverityColor(alert.severity)}20`,
                        color: alert.isAcknowledged ? '#6b7280' : getSeverityColor(alert.severity)
                      }}>
                        {alert.isAcknowledged ? 'Acknowledged' : alert.severity}
                      </span>
                    </div>
                    {!alert.isAcknowledged && (
                      <button
                        onClick={() => handleAcknowledge(alert.id)}
                        style={styles.ackBtn}>
                        Acknowledge
                      </button>
                    )}
                  </div>
                  <p style={styles.incidentDesc}>{alert.message}</p>
                  <div style={styles.incidentMeta}>
                    <span>📍 {alert.junctionName}</span>
                    <span>🕐 {new Date(alert.createdAt).toLocaleString()}</span>
                    {alert.acknowledgedBy && <span>✓ {alert.acknowledgedBy}</span>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* RIGHT — Report Form */}
        <div style={styles.rightCol}>
          <div style={styles.card}>
            <h3 style={styles.cardTitle}>📝 Report Incident</h3>

            <form onSubmit={handleReport} style={styles.form}>
              <div style={styles.formGroup}>
                <label style={styles.label}>Junction</label>
                <select
                  value={formJunction}
                  onChange={e => setFormJunction(e.target.value)}
                  style={styles.select}
                  required>
                  <option value="">Select junction...</option>
                  {junctions.map(j => (
                    <option key={j.id} value={j.id}>{j.name}</option>
                  ))}
                </select>
              </div>

              <div style={styles.formGroup}>
                <label style={styles.label}>Incident Type</label>
                <select
                  value={formType}
                  onChange={e => setFormType(e.target.value)}
                  style={styles.select}
                  required>
                  <option value="">Select type...</option>
                  <option value="Accident">Accident</option>
                  <option value="Breakdown">Vehicle Breakdown</option>
                  <option value="Congestion">Severe Congestion</option>
                  <option value="Violation">Traffic Violation</option>
                  <option value="Roadwork">Roadwork</option>
                  <option value="Other">Other</option>
                </select>
              </div>

              <div style={styles.formGroup}>
                <label style={styles.label}>Severity</label>
                <select
                  value={formSeverity}
                  onChange={e => setFormSeverity(e.target.value)}
                  style={styles.select}>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>

              <div style={styles.formGroup}>
                <label style={styles.label}>Description</label>
                <textarea
                  value={formDesc}
                  onChange={e => setFormDesc(e.target.value)}
                  placeholder="Describe the incident..."
                  style={styles.textarea}
                  rows={4}
                  required
                />
              </div>

              <button type="submit" style={styles.submitBtn}>
                Report Incident
              </button>

              {formMsg && (
                <div style={{
                  marginTop: '10px',
                  fontSize: '13px',
                  color: formMsg.startsWith('✅') ? '#22c55e' : '#ef4444',
                  textAlign: 'center'
                }}>
                  {formMsg}
                </div>
              )}
            </form>
          </div>
        </div>
      </div>
    </Layout>
  );
}

const styles = {
  container: {
    minHeight: '100vh',
    background: '#0a0e1a',
    color: '#ffffff',
    fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif"
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
  loading: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100vh',
    color: '#4da6ff',
    fontSize: '18px',
    background: '#0a0e1a'
  },
  header: {
    background: '#111827',
    borderBottom: '1px solid #1f2937',
    padding: '16px 24px',
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center'
  },
  headerLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '16px'
  },
  backBtn: {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(255,255,255,0.1)',
    borderRadius: '6px',
    padding: '8px 16px',
    color: '#9ca3af',
    cursor: 'pointer',
    fontSize: '14px'
  },
  title: {
    margin: 0,
    fontSize: '20px',
    color: '#f9fafb'
  },
  badges: {
    display: 'flex',
    gap: '10px'
  },
  badge: {
    padding: '4px 12px',
    borderRadius: '20px',
    fontSize: '12px',
    fontWeight: '700'
  },
  content: {
    display: 'flex',
    gap: '20px',
    padding: '20px',
    alignItems: 'flex-start'
  },
  leftCol: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    gap: '12px'
  },
  rightCol: {
    width: '320px'
  },
  tabs: {
    display: 'flex',
    gap: '8px'
  },
  tab: {
    padding: '8px 16px',
    borderRadius: '6px',
    fontSize: '13px',
    fontWeight: '600',
    cursor: 'pointer'
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: '10px'
  },
  empty: {
    textAlign: 'center',
    padding: '40px',
    color: '#6b7280',
    fontSize: '14px'
  },
  incidentCard: {
    background: '#111827',
    border: '1px solid #1f2937',
    borderRadius: '8px',
    padding: '14px'
  },
  incidentHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: '8px'
  },
  incidentType: {
    fontSize: '14px',
    fontWeight: '700',
    color: '#f9fafb',
    marginRight: '8px'
  },
  severityBadge: {
    padding: '2px 8px',
    borderRadius: '4px',
    fontSize: '11px',
    fontWeight: '700'
  },
  resolveBtn: {
    background: 'rgba(34,197,94,0.15)',
    border: '1px solid #22c55e',
    borderRadius: '5px',
    padding: '4px 12px',
    color: '#22c55e',
    fontSize: '12px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  ackBtn: {
    background: 'rgba(77,166,255,0.15)',
    border: '1px solid #4da6ff',
    borderRadius: '5px',
    padding: '4px 12px',
    color: '#4da6ff',
    fontSize: '12px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  incidentDesc: {
    margin: '0 0 8px 0',
    fontSize: '13px',
    color: '#d1d5db'
  },
  incidentMeta: {
    display: 'flex',
    gap: '16px',
    fontSize: '11px',
    color: '#6b7280',
    flexWrap: 'wrap'
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
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '14px'
  },
  formGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '6px'
  },
  label: {
    fontSize: '11px',
    color: '#9ca3af',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    fontWeight: '600'
  },
  select: {
    background: '#1f2937',
    border: '1px solid #374151',
    borderRadius: '6px',
    padding: '8px 12px',
    color: '#ffffff',
    fontSize: '13px'
  },
  textarea: {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(46,117,182,0.3)',
    borderRadius: '6px',
    padding: '10px 12px',
    color: '#ffffff',
    fontSize: '13px',
    resize: 'vertical',
    fontFamily: 'inherit'
  },
  submitBtn: {
    background: '#2563eb',
    border: 'none',
    borderRadius: '6px',
    padding: '12px',
    color: '#ffffff',
    fontSize: '14px',
    fontWeight: '700',
    cursor: 'pointer'
  }
};