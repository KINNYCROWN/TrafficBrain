import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { authAPI } from '../services/api';
import { useAuth } from '../context/AuthContext';
import Layout from '../components/Layout';

export default function Users() {
  const navigate = useNavigate();
  const { isAdmin } = useAuth();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('list');

  const [formFullName, setFormFullName] = useState('');
  const [formEmail, setFormEmail] = useState('');
  const [formPassword, setFormPassword] = useState('');
  const [formRole, setFormRole] = useState('Viewer');
  const [formDistrict, setFormDistrict] = useState('');
  const [formBadge, setFormBadge] = useState('');
  const [formPhone, setFormPhone] = useState('');
  const [formMsg, setFormMsg] = useState('');
  const [formError, setFormError] = useState('');

  const districts = [
    'Lilongwe', 'Blantyre', 'Mzuzu', 'Zomba',
    'Kasungu', 'Mzimba', 'Salima', 'Dedza',
    'Mangochi', 'Mulanje', 'Thyolo', 'Chiradzulu',
    'Machinga', 'Balaka', 'Ntcheu', 'Nkhotakota',
    'Rumphi', 'Karonga', 'Chitipa', 'Nkhata Bay',
    'Dowa', 'Ntchisi', 'Mchinji', 'Lilongwe Rural',
    'Chikwawa', 'Nsanje', 'Phalombe', 'Likoma'
  ];

  useEffect(() => {
    if (!isAdmin()) {
      navigate('/');
      return;
    }
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      const res = await authAPI.getUsers();
      setUsers(res.data);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    setFormMsg('');
    setFormError('');

    if (!formFullName || !formEmail || !formPassword) {
      setFormError('Full name, email and password are required');
      return;
    }

    if (formPassword.length < 8) {
      setFormError('Password must be at least 8 characters');
      return;
    }

    try {
      await authAPI.register({
        fullName: formFullName,
        email: formEmail,
        password: formPassword,
        role: formRole,
        district: formDistrict,
        badgeNumber: formBadge,
        phoneNumber: formPhone
      });

      setFormMsg(`✅ User ${formFullName} registered successfully`);
      setFormFullName('');
      setFormEmail('');
      setFormPassword('');
      setFormRole('Viewer');
      setFormDistrict('');
      setFormBadge('');
      setFormPhone('');

      fetchUsers();
    } catch (err) {
      setFormError(err.response?.data?.message || '❌ Failed to register user');
    }
  };

  const handleDeactivate = async (id, name) => {
    if (!window.confirm(`Deactivate user ${name}?`)) return;
    try {
      await authAPI.deactivateUser(id);
      setUsers(prev => prev.map(u =>
        u.id === id ? { ...u, isActive: false } : u
      ));
    } catch (err) {
      console.error(err);
    }
  };

  const getRoleColor = (role) => {
    switch (role) {
      case 'Admin': return '#ef4444';
      case 'Supervisor': return '#f97316';
      case 'TrafficOfficer': return '#4da6ff';
      default: return '#6b7280';
    }
  };

  if (loading) return (
    <Layout>
      <div style={styles.loading}>Loading users...</div>
    </Layout>
  );

  return (
    <Layout>
      <div style={styles.pageHeader}>
        <h2 style={styles.pageTitle}>👥 User Management</h2>
        <div style={styles.headerRight}>
          <span style={styles.userCount}>
            {users.filter(u => u.isActive).length} Active Users
          </span>
          <button
            onClick={() => setActiveTab('register')}
            style={styles.addBtn}>
            + Add User
          </button>
        </div>
      </div>

      <div style={styles.tabBar}>
        {['list', 'register'].map(tab => (
          <button
            key={tab}
            onClick={() => setActiveTab(tab)}
            style={{
              ...styles.tab,
              color: activeTab === tab ? '#4da6ff' : '#6b7280',
              borderBottom: activeTab === tab
                ? '2px solid #4da6ff' : '2px solid transparent'
            }}>
            {tab === 'list' ? `All Users (${users.length})` : 'Register New User'}
          </button>
        ))}
      </div>

      <div style={styles.content}>

        {activeTab === 'list' && (
          <div style={styles.tableWrapper}>
            <table style={styles.table} className="responsive-table">
              <thead>
                <tr style={styles.tableHead}>
                  <th style={styles.th}>Full Name</th>
                  <th style={styles.th}>Email</th>
                  <th style={styles.th}>Role</th>
                  <th style={styles.th} className="hide-mobile">District</th>
                  <th style={styles.th} className="hide-mobile">Badge</th>
                  <th style={styles.th} className="hide-mobile">Phone</th>
                  <th style={styles.th}>Status</th>
                  <th style={styles.th} className="hide-mobile">Last Login</th>
                  <th style={styles.th}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {users.map((user, i) => (
                  <tr key={i} style={{
                    ...styles.tableRow,
                    background: i % 2 === 0
                      ? 'rgba(255,255,255,0.02)' : 'transparent',
                    opacity: user.isActive ? 1 : 0.4
                  }}>
                    <td style={styles.td}>
                      <span style={styles.userName}>{user.fullName}</span>
                    </td>
                    <td style={styles.td}>{user.email}</td>
                    <td style={styles.td}>
                      <span style={{
                        ...styles.roleBadge,
                        color: getRoleColor(user.role),
                        background: `${getRoleColor(user.role)}20`
                      }}>
                        {user.role}
                      </span>
                    </td>
                    <td style={styles.td} className="hide-mobile">{user.district || '—'}</td>
                    <td style={styles.td} className="hide-mobile">
                      <span style={styles.badge}>{user.badgeNumber || '—'}</span>
                    </td>
                    <td style={styles.td} className="hide-mobile">{user.phoneNumber || '—'}</td>
                    <td style={styles.td}>
                      <span style={{
                        ...styles.statusBadge,
                        background: user.isActive
                          ? 'rgba(34,197,94,0.15)' : 'rgba(239,68,68,0.15)',
                        color: user.isActive ? '#22c55e' : '#ef4444'
                      }}>
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td style={styles.td} className="hide-mobile">
                      {user.lastLogin
                        ? new Date(user.lastLogin).toLocaleString()
                        : 'Never'}
                    </td>
                    <td style={styles.td}>
                      {user.isActive && user.role !== 'Admin' && (
                        <button
                          onClick={() => handleDeactivate(user.id, user.fullName)}
                          style={styles.deactivateBtn}>
                          Deactivate
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {activeTab === 'register' && (
          <div style={styles.formWrapper}>
            <div style={styles.formCard}>
              <h3 style={styles.formTitle}>Register New User</h3>
              <p style={styles.formSubtitle}>
                Only Admins can register new users. All fields marked * are required.
              </p>

              <form onSubmit={handleRegister} style={styles.form}>
                <div style={styles.formRow} className="form-row">
                  <div style={styles.formGroup}>
                    <label style={styles.label}>Full Name *</label>
                    <input
                      type="text"
                      value={formFullName}
                      onChange={e => setFormFullName(e.target.value)}
                      placeholder="e.g. John Banda"
                      style={styles.input}
                      required
                    />
                  </div>
                  <div style={styles.formGroup}>
                    <label style={styles.label}>Email Address *</label>
                    <input
                      type="email"
                      value={formEmail}
                      onChange={e => setFormEmail(e.target.value)}
                      placeholder="officer@trafficbrain.mw"
                      style={styles.input}
                      required
                    />
                  </div>
                </div>

                <div style={styles.formRow} className="form-row">
                  <div style={styles.formGroup}>
                    <label style={styles.label}>Password * (min 8 characters)</label>
                    <input
                      type="password"
                      value={formPassword}
                      onChange={e => setFormPassword(e.target.value)}
                      placeholder="Minimum 8 characters"
                      style={styles.input}
                      required
                    />
                  </div>
                  <div style={styles.formGroup}>
                    <label style={styles.label}>Role *</label>
                    <select
                      value={formRole}
                      onChange={e => setFormRole(e.target.value)}
                      style={styles.select}>
                      <option value="Viewer">Viewer — View only</option>
                      <option value="TrafficOfficer">Traffic Officer — Control junctions</option>
                      <option value="Supervisor">Supervisor — All districts</option>
                      <option value="Admin">Admin — Full access</option>
                    </select>
                  </div>
                </div>

                <div style={styles.formRow} className="form-row">
                  <div style={styles.formGroup}>
                    <label style={styles.label}>District</label>
                    <select
                      value={formDistrict}
                      onChange={e => setFormDistrict(e.target.value)}
                      style={styles.select}>
                      <option value="">Select district...</option>
                      {districts.map(d => (
                        <option key={d} value={d}>{d}</option>
                      ))}
                    </select>
                  </div>
                  <div style={styles.formGroup}>
                    <label style={styles.label}>Badge Number</label>
                    <input
                      type="text"
                      value={formBadge}
                      onChange={e => setFormBadge(e.target.value)}
                      placeholder="e.g. TB-OFF-002"
                      style={styles.input}
                    />
                  </div>
                </div>

                <div style={styles.formRow} className="form-row">
                  <div style={styles.formGroup}>
                    <label style={styles.label}>Phone Number</label>
                    <input
                      type="text"
                      value={formPhone}
                      onChange={e => setFormPhone(e.target.value)}
                      placeholder="e.g. +265999000002"
                      style={styles.input}
                    />
                  </div>
                  <div style={styles.formGroup} />
                </div>

                <div style={styles.roleInfo}>
                  <h4 style={styles.roleInfoTitle}>Role Permissions</h4>
                  <div style={styles.roleGrid} className="form-row">
                    {[
                      { role: 'Viewer', color: '#6b7280', desc: 'View dashboard and analytics only. Cannot control junctions or report incidents.' },
                      { role: 'Traffic Officer', color: '#4da6ff', desc: 'View all data, control junctions in their assigned district, report and resolve incidents.' },
                      { role: 'Supervisor', color: '#f97316', desc: 'Control junctions across all districts, manage incidents, view audit logs.' },
                      { role: 'Admin', color: '#ef4444', desc: 'Full system access including user management, system settings, and all audit logs.' },
                    ].map((r, i) => (
                      <div key={i} style={styles.roleItem}>
                        <span style={{ ...styles.roleLabel, color: r.color }}>{r.role}</span>
                        <span style={styles.roleDesc}>{r.desc}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {formError && (
                  <div style={styles.errorMsg}>{formError}</div>
                )}

                {formMsg && (
                  <div style={styles.successMsg}>{formMsg}</div>
                )}

                <div style={styles.formActions}>
                  <button type="submit" style={styles.submitBtn}>
                    Register User
                  </button>
                  <button
                    type="button"
                    onClick={() => setActiveTab('list')}
                    style={styles.cancelBtn}>
                    Cancel
                  </button>
                </div>
              </form>
            </div>
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
  userCount: {
    fontSize: '13px',
    color: '#6b7280'
  },
  addBtn: {
    background: '#2563eb',
    border: 'none',
    borderRadius: '6px',
    padding: '8px 16px',
    color: '#ffffff',
    fontSize: '13px',
    fontWeight: '700',
    cursor: 'pointer'
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
    whiteSpace: 'nowrap'
  },
  content: {
    padding: '20px'
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
  userName: {
    fontWeight: '600',
    color: '#f9fafb'
  },
  roleBadge: {
    padding: '3px 10px',
    borderRadius: '4px',
    fontSize: '11px',
    fontWeight: '700'
  },
  badge: {
    fontFamily: 'monospace',
    fontSize: '12px',
    color: '#9ca3af'
  },
  statusBadge: {
    padding: '2px 8px',
    borderRadius: '4px',
    fontSize: '11px',
    fontWeight: '700'
  },
  deactivateBtn: {
    background: 'rgba(239,68,68,0.1)',
    border: '1px solid rgba(239,68,68,0.3)',
    borderRadius: '5px',
    padding: '4px 10px',
    color: '#ef4444',
    fontSize: '11px',
    fontWeight: '600',
    cursor: 'pointer'
  },
  formWrapper: {
    display: 'flex',
    justifyContent: 'center'
  },
  formCard: {
    background: '#111827',
    border: '1px solid #1f2937',
    borderRadius: '12px',
    padding: '32px',
    width: '100%',
    maxWidth: '800px'
  },
  formTitle: {
    margin: '0 0 8px 0',
    fontSize: '18px',
    color: '#f9fafb'
  },
  formSubtitle: {
    margin: '0 0 24px 0',
    fontSize: '13px',
    color: '#6b7280'
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '16px'
  },
  formRow: {
    display: 'grid',
    gridTemplateColumns: '1fr 1fr',
    gap: '16px'
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
  input: {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(46,117,182,0.3)',
    borderRadius: '6px',
    padding: '10px 14px',
    color: '#ffffff',
    fontSize: '13px'
  },
  select: {
    background: '#1f2937',
    border: '1px solid #374151',
    borderRadius: '6px',
    padding: '10px 14px',
    color: '#ffffff',
    fontSize: '13px'
  },
  roleInfo: {
    background: 'rgba(255,255,255,0.02)',
    border: '1px solid #1f2937',
    borderRadius: '8px',
    padding: '16px',
    marginTop: '8px'
  },
  roleInfoTitle: {
    margin: '0 0 12px 0',
    fontSize: '12px',
    color: '#6b7280',
    textTransform: 'uppercase',
    letterSpacing: '1px'
  },
  roleGrid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(2, 1fr)',
    gap: '12px'
  },
  roleItem: {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px'
  },
  roleLabel: {
    fontSize: '12px',
    fontWeight: '700',
    textTransform: 'uppercase',
    letterSpacing: '1px'
  },
  roleDesc: {
    fontSize: '12px',
    color: '#6b7280',
    lineHeight: '1.5'
  },
  errorMsg: {
    background: 'rgba(239,68,68,0.1)',
    border: '1px solid rgba(239,68,68,0.3)',
    borderRadius: '6px',
    padding: '10px',
    color: '#f87171',
    fontSize: '13px',
    textAlign: 'center'
  },
  successMsg: {
    background: 'rgba(34,197,94,0.1)',
    border: '1px solid rgba(34,197,94,0.3)',
    borderRadius: '6px',
    padding: '10px',
    color: '#4ade80',
    fontSize: '13px',
    textAlign: 'center'
  },
  formActions: {
    display: 'flex',
    gap: '12px',
    marginTop: '8px',
    flexWrap: 'wrap'
  },
  submitBtn: {
    flex: 1,
    background: '#2563eb',
    border: 'none',
    borderRadius: '6px',
    padding: '12px',
    color: '#ffffff',
    fontSize: '14px',
    fontWeight: '700',
    cursor: 'pointer'
  },
  cancelBtn: {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(255,255,255,0.1)',
    borderRadius: '6px',
    padding: '12px 24px',
    color: '#9ca3af',
    fontSize: '14px',
    cursor: 'pointer'
  }
};
