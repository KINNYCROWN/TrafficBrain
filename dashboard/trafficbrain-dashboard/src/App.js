import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';
import Login from './pages/Login';
import National from './pages/National';
import JunctionDetail from './pages/JunctionDetail';
import Analytics from './pages/Analytics';
import Incidents from './pages/Incidents';
import SystemHealth from './pages/SystemHealth';
import Users from './pages/Users';

function ProtectedRoute({ children }) {
  const { user, loading } = useAuth();

  if (loading) return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      height: '100vh',
      background: '#0a0e1a',
      color: '#4da6ff',
      fontSize: '18px',
      fontFamily: 'Segoe UI, sans-serif'
    }}>
      🚦 Loading TRAFFICBRAIN...
    </div>
  );

  if (!user) return <Navigate to="/login" replace />;
  return children;
}

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={
            <ProtectedRoute>
              <National />
            </ProtectedRoute>
          } />
          <Route path="/junction/:id" element={
            <ProtectedRoute>
              <JunctionDetail />
            </ProtectedRoute>
          } />
          <Route path="/analytics" element={
            <ProtectedRoute>
              <Analytics />
            </ProtectedRoute>
          } />
          <Route path="/incidents" element={
            <ProtectedRoute>
              <Incidents />
            </ProtectedRoute>
          } />
          <Route path="/system" element={
            <ProtectedRoute>
              <SystemHealth />
            </ProtectedRoute>
          } />
          <Route path="/users" element={
            <ProtectedRoute>
              <Users />
            </ProtectedRoute>
          } />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;