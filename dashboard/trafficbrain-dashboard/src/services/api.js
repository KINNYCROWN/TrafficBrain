import axios from 'axios';

const API_URL = 'http://localhost:5275';

// Create axios instance
const api = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json'
  }
});

// Attach JWT token to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle token expiry
api.interceptors.response.use(
  response => response,
  async error => {
    const original = error.config;

    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const response = await axios.post(`${API_URL}/api/auth/refresh`, {
          refreshToken
        });

        const { accessToken, refreshToken: newRefreshToken } = response.data;
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', newRefreshToken);

        original.headers.Authorization = `Bearer ${accessToken}`;
        return api(original);
      } catch {
        localStorage.clear();
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  }
);

// ─────────────────────────────────────────
// AUTH
// ─────────────────────────────────────────
export const authAPI = {
  login: (email, password) =>
    api.post('/api/auth/login', { email, password }),
  logout: () =>
    api.post('/api/auth/logout'),
  me: () =>
    api.get('/api/auth/me'),
  getUsers: () =>
    api.get('/api/auth/users'),
  register: (data) =>
    api.post('/api/auth/register', data),
  deactivateUser: (id) =>
    api.put(`/api/auth/users/${id}/deactivate`),
  changePassword: (data) =>
    api.put('/api/auth/changepassword', data)
};

// ─────────────────────────────────────────
// JUNCTIONS
// ─────────────────────────────────────────
export const junctionsAPI = {
  getAll: () =>
    api.get('/api/junctions'),
  getById: (id) =>
    api.get(`/api/junctions/${id}`),
  getByDistrict: (district) =>
    api.get(`/api/junctions/district/${district}`),
  getStats: () =>
    api.get('/api/junctions/stats'),
  getOnline: () =>
    api.get('/api/junctions/online'),
  create: (data) =>
    api.post('/api/junctions', data),
  update: (id, data) =>
    api.put(`/api/junctions/${id}`, data),
  deactivate: (id) =>
    api.delete(`/api/junctions/${id}`)
};

// ─────────────────────────────────────────
// TRAFFIC
// ─────────────────────────────────────────
export const trafficAPI = {
  postVehicleCount: (data) =>
    api.post('/api/traffic/vehiclecount', data),
  getLiveData: (junctionId) =>
    api.get(`/api/traffic/${junctionId}/live`),
  getHistoricalCounts: (junctionId, hours = 1) =>
    api.get(`/api/traffic/${junctionId}/counts?hours=${hours}`),
  overrideSignal: (data) =>
    api.post('/api/traffic/signal/override', data),
  returnToAuto: (junctionId) =>
    api.post('/api/traffic/signal/auto', junctionId),
  getOverrideHistory: (junctionId) =>
    api.get(`/api/traffic/${junctionId}/overrides`)
};

// ─────────────────────────────────────────
// EMERGENCY
// ─────────────────────────────────────────
export const emergencyAPI = {
  detect: (data) =>
    api.post('/api/emergency/detect', data),
  getActive: () =>
    api.get('/api/emergency/active'),
  resolve: (id) =>
    api.put(`/api/emergency/${id}/resolve`),
  getHistory: () =>
    api.get('/api/emergency/history'),
  activateVIP: (data) =>
    api.post('/api/emergency/vip', data)
};

// ─────────────────────────────────────────
// INCIDENTS
// ─────────────────────────────────────────
export const incidentsAPI = {
  report: (data) =>
    api.post('/api/incidents', data),
  getActive: () =>
    api.get('/api/incidents/active'),
  resolve: (id) =>
    api.put(`/api/incidents/${id}/resolve`),
  getHistory: (district, days) =>
    api.get(`/api/incidents/history?district=${district || ''}&days=${days || 7}`),
  getAlerts: () =>
    api.get('/api/incidents/alerts'),
  acknowledgeAlert: (id) =>
    api.put(`/api/incidents/alerts/${id}/acknowledge`)
};

// ─────────────────────────────────────────
// ANALYTICS
// ─────────────────────────────────────────
export const analyticsAPI = {
  getNational: () =>
    api.get('/api/analytics/national'),
  getPerformance: (junctionId) =>
    api.get(`/api/analytics/performance/${junctionId}`),
  getPeakHours: (junctionId) =>
    api.get(`/api/analytics/peakhours/${junctionId}`),
  getDistrict: (district) =>
    api.get(`/api/analytics/district/${district}`),
  getComparison: (junctionId) =>
    api.get(`/api/analytics/comparison/${junctionId}`),
  getEnvironment: () =>
    api.get('/api/analytics/environment'),
  exportData: (junctionId, days) =>
    api.get(`/api/analytics/export/${junctionId}?days=${days || 7}`, {
      responseType: 'blob'
    }),
  getPredictions: (junctionId) =>
    api.get(`/api/analytics/predictions/${junctionId}`),
  getVehicleBreakdown: (hours) =>
    api.get(`/api/analytics/vehicles/breakdown?hours=${hours || 24}`)
};

// ─────────────────────────────────────────
// WEATHER
// ─────────────────────────────────────────
export const weatherAPI = {
  getAll: () =>
    api.get('/api/weather/all'),
  getCity: (city) =>
    api.get(`/api/weather/${city}`),
  getSignalAdjustment: (city) =>
    api.get(`/api/weather/signal/${city}`)
};

// ─────────────────────────────────────────
// SYSTEM
// ─────────────────────────────────────────
export const systemAPI = {
  getHealth: () =>
    api.get('/api/system/health'),
  getStats: () =>
    api.get('/api/system/stats'),
  getLogs: (page, pageSize, action, role) =>
    api.get(`/api/system/logs?page=${page || 1}&pageSize=${pageSize || 50}&action=${action || ''}&role=${role || ''}`),
  getJunctionStatus: () =>
    api.get('/api/system/junctions/status'),
  getDistricts: () =>
    api.get('/api/system/districts'),
  getRegions: () =>
    api.get('/api/system/regions')
};

export default api;