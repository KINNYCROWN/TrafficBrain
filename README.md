# 🚦 TRAFFICBRAIN
### Autonomous Real-Time Traffic Intelligence and Control System for Malawi

![Version](https://img.shields.io/badge/version-2.0.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)
![Status](https://img.shields.io/badge/status-Active-brightgreen)

---

## 📋 Overview

TRAFFICBRAIN is a comprehensive intelligent traffic management system designed for urban road networks across Malawi. It combines computer vision, artificial intelligence, real-time communication, and physical hardware to monitor, analyse, and dynamically control traffic flow without human intervention.

The system demonstrates a **24% reduction in average vehicle wait time** compared to fixed-timer control, validated through a high-fidelity digital twin simulation of Lilongwe's road network.

---

## 🏗️ System Architecture
---

## 🔧 Technology Stack

| Layer | Technology |
|---|---|
| Vehicle Detection | Python, OpenCV, YOLOv8 |
| Traffic Simulation | SUMO + TraCI API |
| AI Controller | Python, Stable-Baselines3 |
| Backend API | ASP.NET Core 8, C# |
| Real-time Updates | SignalR |
| Database | PostgreSQL 18 |
| Frontend Dashboard | React, Leaflet.js, Recharts |
| Authentication | JWT + BCrypt |
| Weather | OpenWeatherMap API |
| SMS Alerts | Africa's Talking API |
| Physical Prototype | Arduino Uno + LEDs |

---

## 📁 Project Structure
---

## 🚀 Features

### Core Modules
- **Module 1 — Vehicle Detection**: YOLOv8-based real-time detection of cars, motorcycles, buses, trucks and pedestrians with speed estimation and congestion scoring
- **Module 2 — SUMO Digital Twin**: High-fidelity simulation of real Malawian road networks using OpenStreetMap data
- **Module 3 — AI Traffic Controller**: Adaptive signal control that dynamically adjusts green phase durations based on real-time queue lengths
- **Module 4 — Backend API**: RESTful API with 30+ endpoints, JWT authentication, role-based access control, and audit logging
- **Module 5 — Dashboard**: Responsive professional dashboard with national map, live junction monitoring, analytics, and incident management
- **Module 6 — Physical Prototype**: Arduino-controlled LED traffic light synchronized with system decisions
- **Module 7 — Emergency Corridor**: Automatic green wave clearing for emergency vehicles across multiple junctions

### Additional Features
- 🌤️ **Live Weather Integration**: Real-time weather data for Lilongwe, Blantyre, Mzuzu and Zomba with automatic signal timing adjustments
- 📱 **SMS Alerts**: Africa's Talking integration for emergency and congestion SMS notifications
- 👥 **User Management**: Role-based access (Admin, Supervisor, Traffic Officer, Viewer)
- 📊 **Analytics**: AI vs Fixed Timer comparison, peak hour analysis, environmental impact tracking
- 🔒 **Security**: JWT authentication, BCrypt password hashing, audit logging, environment-based configuration

---

## 📊 Performance Results

| Metric | Fixed Timer | TRAFFICBRAIN AI |
|---|---|---|
| Average Wait Time | 2.06s | 1.56s |
| Improvement | — | **24% reduction** |
| Vehicles Processed | 2,591 | 2,591 |
| Simulation Steps | 1,000 | 1,000 |

*Results from SUMO simulation of Lilongwe road network*

---

## 🗄️ Database Schema

15 tables covering:
- Users, Regions, Districts, Cities, Junctions, Lanes
- VehicleCounts, SignalPhases, Incidents, EmergencyEvents
- Alerts, ViolationEvents, JunctionOverrides
- PerformanceMetrics, TrafficPredictions
- WeatherConditions, ShiftRecords, SmsAlerts, SystemLogs

---

## 🔐 User Roles

| Role | Permissions |
|---|---|
| Admin | Full system access, user management, audit logs |
| Supervisor | Control all districts, view logs, export reports |
| Traffic Officer | Control assigned district junctions, report incidents |
| Viewer | View dashboard and analytics only |

---

## ⚙️ Setup Instructions

### Prerequisites
- Python 3.11+
- .NET 8 SDK
- Node.js 20 LTS
- PostgreSQL 18
- SUMO 1.27+
- Arduino IDE (for prototype)

### 1. Clone the repository
```bash
git clone https://github.com/KINNYCROWN/TrafficBrain.git
cd TrafficBrain
```

### 2. Backend Setup
```bash
cd backend/TrafficBrainAPI
cp appsettings.json appsettings.Local.json
# Edit appsettings.Local.json with your database password and API keys
dotnet restore
dotnet ef database update
dotnet run
```

### 3. Python Dependencies
```bash
pip install ultralytics opencv-python torch torchvision stable-baselines3 traci paho-mqtt requests
```

### 4. Dashboard Setup
```bash
cd dashboard/trafficbrain-dashboard
npm install
npm start
```

### 5. Default Login
- **Email**: admin@trafficbrain.mw
- **Password**: Admin@2026

---

## 🎮 Running the System

Start all components in separate terminals:

**Terminal 1 — API:**
```bash
cd backend/TrafficBrainAPI
dotnet run
```

**Terminal 2 — Dashboard:**
```bash
cd dashboard/trafficbrain-dashboard
npm start
```

**Terminal 3 — Vehicle Detection:**
```bash
cd detection
python detector.py
```

**Terminal 4 — SUMO Simulation (optional):**
```bash
cd simulation/2026-06-13-13-24-10
python trafficbrain_controller.py Adaptive
```

---

## 🌍 Deployment Coverage

Designed for all urban centres in Malawi with traffic signals:
- **Lilongwe** — Capital city (3 junctions)
- **Blantyre** — Commercial capital (2 junctions)
- **Mzuzu** — Northern capital (1 junction)
- **Zomba** — Former capital (1 junction)

---

## 👨‍💻 Developer

**Kingsley Banda**
- Student ID: P00201976
- BSc (Hons) Computing — Level 6
- NACIT Lilongwe Campus / University of Greenwich
- Supervisor: Kalitela

---

## 📄 License

This project was developed as a final year computing project at NACIT Lilongwe Campus.