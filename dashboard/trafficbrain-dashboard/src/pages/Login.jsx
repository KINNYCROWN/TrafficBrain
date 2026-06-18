import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      setError('Invalid email or password');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.container}>
      {/* Background */}
      <div style={styles.bg} />

      {/* Login Card */}
      <div style={styles.card}>
        {/* Logo */}
        <div style={styles.logoSection}>
          <div style={styles.logoIcon}>🚦</div>
          <h1 style={styles.logoText}>TRAFFICBRAIN</h1>
          <p style={styles.logoSub}>Malawi Intelligent Traffic Control System</p>
        </div>

        {/* Form */}
        <form onSubmit={handleLogin} style={styles.form}>
          <div style={styles.inputGroup}>
            <label style={styles.label}>Email Address</label>
            <input
              type="email"
              value={email}
              onChange={e => setEmail(e.target.value)}
              placeholder="officer@trafficbrain.mw"
              style={styles.input}
              required
            />
          </div>

          <div style={styles.inputGroup}>
            <label style={styles.label}>Password</label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Enter your password"
              style={styles.input}
              required
            />
          </div>

          {error && (
            <div style={styles.error}>
              ⚠️ {error}
            </div>
          )}

          <button
            type="submit"
            style={{
              ...styles.button,
              opacity: loading ? 0.7 : 1,
              cursor: loading ? 'not-allowed' : 'pointer'
            }}
            disabled={loading}
          >
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        {/* Demo credentials */}
        <div style={styles.demo}>
          <p style={styles.demoTitle}>Demo Credentials</p>
          <p style={styles.demoText}>Email: admin@trafficbrain.mw</p>
          <p style={styles.demoText}>Password: Admin@2026</p>
        </div>

        <p style={styles.footer}>
          TRAFFICBRAIN v2.0 — National Traffic Intelligence System
        </p>
      </div>
    </div>
  );
}

const styles = {
  container: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'linear-gradient(135deg, #0a0e1a 0%, #0d1b2a 50%, #1a1f35 100%)',
    fontFamily: "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif",
    position: 'relative',
    overflow: 'hidden'
  },
  bg: {
    position: 'absolute',
    inset: 0,
    backgroundImage: `radial-gradient(circle at 20% 50%, rgba(46, 117, 182, 0.15) 0%, transparent 50%),
                      radial-gradient(circle at 80% 20%, rgba(0, 200, 150, 0.1) 0%, transparent 40%)`,
    pointerEvents: 'none'
  },
  card: {
    background: 'rgba(17, 24, 39, 0.95)',
    border: '1px solid rgba(46, 117, 182, 0.3)',
    borderRadius: '16px',
    padding: '48px',
    width: '100%',
    maxWidth: '420px',
    boxShadow: '0 25px 50px rgba(0,0,0,0.5)',
    position: 'relative',
    zIndex: 1
  },
  logoSection: {
    textAlign: 'center',
    marginBottom: '36px'
  },
  logoIcon: {
    fontSize: '48px',
    marginBottom: '12px',
    display: 'block'
  },
  logoText: {
    fontSize: '28px',
    fontWeight: '800',
    color: '#4da6ff',
    letterSpacing: '4px',
    margin: '0 0 8px 0'
  },
  logoSub: {
    fontSize: '13px',
    color: '#6b7280',
    margin: 0
  },
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: '20px'
  },
  inputGroup: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px'
  },
  label: {
    fontSize: '13px',
    fontWeight: '600',
    color: '#9ca3af',
    textTransform: 'uppercase',
    letterSpacing: '1px'
  },
  input: {
    background: 'rgba(255,255,255,0.05)',
    border: '1px solid rgba(46, 117, 182, 0.3)',
    borderRadius: '8px',
    padding: '12px 16px',
    color: '#ffffff',
    fontSize: '14px',
    outline: 'none',
    transition: 'border-color 0.2s'
  },
  error: {
    background: 'rgba(239, 68, 68, 0.1)',
    border: '1px solid rgba(239, 68, 68, 0.3)',
    borderRadius: '8px',
    padding: '12px',
    color: '#f87171',
    fontSize: '14px',
    textAlign: 'center'
  },
  button: {
    background: 'linear-gradient(135deg, #1d4ed8, #2563eb)',
    border: 'none',
    borderRadius: '8px',
    padding: '14px',
    color: '#ffffff',
    fontSize: '15px',
    fontWeight: '700',
    letterSpacing: '1px',
    marginTop: '8px',
    transition: 'all 0.2s'
  },
  demo: {
    marginTop: '24px',
    padding: '16px',
    background: 'rgba(46, 117, 182, 0.1)',
    borderRadius: '8px',
    border: '1px solid rgba(46, 117, 182, 0.2)',
    textAlign: 'center'
  },
  demoTitle: {
    fontSize: '12px',
    color: '#4da6ff',
    fontWeight: '700',
    textTransform: 'uppercase',
    letterSpacing: '1px',
    margin: '0 0 8px 0'
  },
  demoText: {
    fontSize: '13px',
    color: '#9ca3af',
    margin: '4px 0',
    fontFamily: 'monospace'
  },
  footer: {
    textAlign: 'center',
    fontSize: '11px',
    color: '#374151',
    marginTop: '24px',
    marginBottom: 0
  }
};