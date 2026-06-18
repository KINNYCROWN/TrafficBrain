import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Layout({ children, title }) {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  const navItems = [
    { path: '/', label: 'National Map', icon: '🗺️' },
    { path: '/analytics', label: 'Analytics', icon: '📊' },
    { path: '/incidents', label: 'Incidents', icon: '⚠️' },
    { path: '/system', label: 'System', icon: '⚙️' },
    ...(user?.role === 'Admin' ? [{ path: '/users', label: 'Users', icon: '👥' }] : [])
  ];

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div style={styles.wrapper}>

      {/* ── TOP NAVBAR ── */}
      <nav style={styles.navbar}>
        <div style={styles.navLeft}>
          <span style={styles.logo}>🚦 TRAFFICBRAIN</span>
        </div>

        {/* Desktop nav links */}
        <div style={styles.navCenter}>
          {navItems.map((item, i) => (
            <button
              key={i}
              onClick={() => navigate(item.path)}
              style={{
                ...styles.navLink,
                background: location.pathname === item.path
                  ? 'rgba(37,99,235,0.2)' : 'transparent',
                color: location.pathname === item.path
                  ? '#4da6ff' : '#9ca3af',
                borderBottom: location.pathname === item.path
                  ? '2px solid #4da6ff' : '2px solid transparent'
              }}>
              <span>{item.icon}</span>
              <span style={styles.navLinkLabel}>{item.label}</span>
            </button>
          ))}
        </div>

        <div style={styles.navRight}>
          {/* User info */}
          <div style={styles.userChip}>
            <div style={styles.userAvatar}>
              {user?.fullName?.charAt(0) || 'U'}
            </div>
            <div style={styles.userDetails}>
              <span style={styles.userName}>{user?.fullName?.split(' ')[0]}</span>
              <span style={styles.userRole}>{user?.role}</span>
            </div>
          </div>

          {/* Logout */}
          <button onClick={handleLogout} style={styles.logoutBtn} title="Logout">
            ⏻
          </button>

          {/* Mobile hamburger */}
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            style={styles.hamburger}>
            {menuOpen ? '✕' : '☰'}
          </button>
        </div>
      </nav>

      {/* ── MOBILE MENU ── */}
      {menuOpen && (
        <div style={styles.mobileMenu}>
          {navItems.map((item, i) => (
            <button
              key={i}
              onClick={() => { navigate(item.path); setMenuOpen(false); }}
              style={{
                ...styles.mobileMenuItem,
                background: location.pathname === item.path
                  ? 'rgba(37,99,235,0.2)' : 'transparent',
                color: location.pathname === item.path
                  ? '#4da6ff' : '#d1d5db'
              }}>
              <span style={styles.mobileMenuIcon}>{item.icon}</span>
              <span>{item.label}</span>
            </button>
          ))}
          <button onClick={handleLogout} style={styles.mobileLogout}>
            ⏻ Logout
          </button>
        </div>
      )}

      {/* ── PAGE CONTENT ── */}
      <main style={styles.main}>
        {children}
      </main>
    </div>
  );
}

const styles = {
  wrapper: {
  display: 'flex',
  flexDirection: 'column',
  height: '100vh',
  background: '#0a0e1a',
  color: '#ffffff',
  fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
  overflow: 'hidden'
},
  navbar: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    background: 'linear-gradient(135deg, #0d1b2a, #1a1f35)',
    borderBottom: '1px solid rgba(46,117,182,0.3)',
    padding: '0 16px',
    height: '52px',
    flexShrink: 0,
    position: 'sticky',
    top: 0,
    zIndex: 2000
  },
  navLeft: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px'
  },
  logo: {
    fontSize: '16px',
    fontWeight: '800',
    color: '#4da6ff',
    letterSpacing: '2px',
    whiteSpace: 'nowrap'
  },
  navCenter: {
    display: 'flex',
    alignItems: 'center',
    gap: '2px',
    '@media (max-width: 768px)': {
      display: 'none'
    }
  },
  navLink: {
    display: 'flex',
    alignItems: 'center',
    gap: '5px',
    padding: '0 12px',
    height: '52px',
    border: 'none',
    cursor: 'pointer',
    fontSize: '13px',
    fontWeight: '600',
    transition: 'all 0.2s'
  },
  navLinkLabel: {
    '@media (max-width: 1024px)': {
      display: 'none'
    }
  },
  navRight: {
    display: 'flex',
    alignItems: 'center',
    gap: '10px'
  },
  userChip: {
    display: 'flex',
    alignItems: 'center',
    gap: '8px',
    padding: '4px 10px',
    background: 'rgba(255,255,255,0.05)',
    borderRadius: '20px',
    border: '1px solid rgba(255,255,255,0.1)'
  },
  userAvatar: {
    width: '26px',
    height: '26px',
    borderRadius: '50%',
    background: '#2563eb',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    fontSize: '12px',
    fontWeight: '700',
    color: '#fff',
    flexShrink: 0
  },
  userDetails: {
    display: 'flex',
    flexDirection: 'column',
    gap: '1px'
  },
  userName: {
    fontSize: '12px',
    fontWeight: '600',
    color: '#e5e7eb',
    lineHeight: 1
  },
  userRole: {
    fontSize: '10px',
    color: '#4da6ff',
    textTransform: 'uppercase',
    letterSpacing: '0.5px',
    lineHeight: 1
  },
  logoutBtn: {
    background: 'rgba(239,68,68,0.1)',
    border: '1px solid rgba(239,68,68,0.2)',
    borderRadius: '6px',
    padding: '6px 10px',
    color: '#ef4444',
    fontSize: '14px',
    cursor: 'pointer'
  },
  hamburger: {
    display: 'none',
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(255,255,255,0.1)',
    borderRadius: '6px',
    padding: '6px 10px',
    color: '#9ca3af',
    fontSize: '16px',
    cursor: 'pointer',
    '@media (max-width: 768px)': {
      display: 'flex'
    }
  },
  mobileMenu: {
    background: '#111827',
    borderBottom: '1px solid #1f2937',
    padding: '8px',
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
    zIndex: 1999
  },
  mobileMenuItem: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '12px 16px',
    borderRadius: '8px',
    border: 'none',
    cursor: 'pointer',
    fontSize: '14px',
    fontWeight: '600',
    textAlign: 'left'
  },
  mobileMenuIcon: {
    fontSize: '18px',
    width: '24px',
    textAlign: 'center'
  },
  mobileLogout: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
    padding: '12px 16px',
    borderRadius: '8px',
    border: 'none',
    cursor: 'pointer',
    fontSize: '14px',
    fontWeight: '600',
    background: 'rgba(239,68,68,0.1)',
    color: '#ef4444',
    marginTop: '4px'
  },
  main: {
  flex: 1,
  overflowY: 'auto',
  overflowX: 'hidden',
  minHeight: 0
}
};