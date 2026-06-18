import React, { createContext, useContext, useState, useEffect } from 'react';
import { authAPI } from '../services/api';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      authAPI.me()
        .then(res => setUser(res.data))
        .catch(() => {
          localStorage.clear();
          setUser(null);
        })
        .finally(() => setLoading(false));
    } else {
      setLoading(false);
    }
  }, []);

  const login = async (email, password) => {
    const res = await authAPI.login(email, password);
    const data = res.data;
    localStorage.setItem('accessToken', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    setUser({
      fullName: data.fullName,
      email: data.email,
      role: data.role,
      district: data.district,
      badgeNumber: data.badgeNumber
    });
    return data;
  };

  const logout = async () => {
    try { await authAPI.logout(); } catch {}
    localStorage.clear();
    setUser(null);
  };

  const hasRole = (...roles) => roles.includes(user?.role);

  const canControl = () =>
    hasRole('Admin', 'Supervisor', 'TrafficOfficer');

  const isAdmin = () => hasRole('Admin');

  return (
    <AuthContext.Provider value={{
      user, loading, login, logout, hasRole, canControl, isAdmin
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}