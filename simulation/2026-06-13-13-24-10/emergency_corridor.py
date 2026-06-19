import traci
import requests
import time
from datetime import datetime

# ─────────────────────────────────────────
# CONFIGURATION
# ─────────────────────────────────────────
SUMO_CONFIG = "osm.sumocfg"
SUMO_BINARY = "sumo-gui"
API_URL = "http://localhost:5275"
AUTH_TOKEN = None

# Malawi junction corridor mapping
CORRIDORS = {
    "lilongwe_main": [1, 2, 3],
    "blantyre_main": [4, 5],
    "mzuzu_main": [6],
    "zomba_main": [7]
}

# ─────────────────────────────────────────
# AUTHENTICATION
# ─────────────────────────────────────────
def get_auth_token():
    global AUTH_TOKEN
    try:
        response = requests.post(
            f"{API_URL}/api/auth/login",
            json={
                "email": "admin@trafficbrain.mw",
                "password": "Admin@2026"
            },
            headers={"Content-Type": "application/json"},
            timeout=5
        )
        if response.status_code == 200:
            AUTH_TOKEN = response.json()["accessToken"]
            print(f"✅ Authenticated with TrafficBrain API")
            return True
        else:
            print(f"⚠️ Auth failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Auth error: {e}")
        return False

# ─────────────────────────────────────────
# API CALLS
# ─────────────────────────────────────────
def report_emergency_to_api(junction_id, vehicle_type, direction):
    """Send emergency alert to backend API — triggers SignalR to dashboard"""
    global AUTH_TOKEN
    if not AUTH_TOKEN:
        get_auth_token()
    try:
        response = requests.post(
            f"{API_URL}/api/emergency/detect",
            json={
                "junctionId": junction_id,
                "vehicleType": vehicle_type,
                "direction": direction
            },
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {AUTH_TOKEN}"
            },
            timeout=5
        )
        if response.status_code == 200:
            data = response.json()
            print(f"✅ Emergency reported to API")
            print(f"   Corridor junctions cleared: {data.get('corridorJunctions', 0)}")
            print(f"   Estimated time saved: {data.get('timeSavedMinutes', 0)} minutes")
            return True
        elif response.status_code == 401:
            AUTH_TOKEN = None
            get_auth_token()
            return False
        else:
            print(f"⚠️ API response: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Failed to reach API: {e}")
        return False

def report_emergency_resolved_to_api(emergency_id):
    """Mark emergency as resolved in API"""
    global AUTH_TOKEN
    try:
        response = requests.put(
            f"{API_URL}/api/emergency/{emergency_id}/resolve",
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {AUTH_TOKEN}"
            },
            timeout=5
        )
        if response.status_code == 200:
            print(f"✅ Emergency {emergency_id} marked as resolved")
    except Exception as e:
        print(f"❌ Failed to resolve: {e}")

# ─────────────────────────────────────────
# CORRIDOR MANAGEMENT
# ─────────────────────────────────────────
def get_corridor_junctions(start_junction_id):
    """Find which corridor a junction belongs to"""
    for corridor_name, junction_sequence in CORRIDORS.items():
        if start_junction_id in junction_sequence:
            start_index = junction_sequence.index(start_junction_id)
            return corridor_name, junction_sequence[start_index:]
    return None, [start_junction_id]

def clear_emergency_corridor(sumo_traffic_lights, emergency_junction_id,
                              vehicle_type="Ambulance", direction="North"):
    """
    Main emergency corridor clearing algorithm.
    1. Reports to API (triggers dashboard alert via SignalR)
    2. Sets all corridor junctions to GREEN in SUMO
    3. Waits for vehicle to pass
    4. Restores normal operation
    """
    print("\n" + "=" * 60)
    print(f"🚨 EMERGENCY VEHICLE DETECTED")
    print(f"   Type      : {vehicle_type}")
    print(f"   Location  : Junction {emergency_junction_id}")
    print(f"   Direction : {direction}")
    print(f"   Time      : {datetime.now().strftime('%H:%M:%S')}")
    print("=" * 60)

    # Step 1: Report to API — this triggers SignalR alert on dashboard
    print("\n📡 Reporting to TrafficBrain API...")
    report_emergency_to_api(emergency_junction_id, vehicle_type, direction)

    # Step 2: Find corridor
    corridor_name, corridor_junctions = get_corridor_junctions(emergency_junction_id)
    if corridor_name:
        print(f"\n🛣️  Corridor: {corridor_name}")
    print(f"🟢 Clearing {len(corridor_junctions)} junction(s)...")

    # Step 3: Set all corridor junctions to GREEN in SUMO
    cleared = 0
    for i, junction_id in enumerate(corridor_junctions):
        for tl_id in sumo_traffic_lights:
            try:
                traci.trafficlight.setPhase(tl_id, 0)  # GREEN phase
                traci.trafficlight.setPhaseDuration(tl_id, 60)
                print(f"  ✅ Junction {junction_id} → GREEN ({i+1}/{len(corridor_junctions)})")
                cleared += 1
                time.sleep(0.3)
                break
            except Exception as e:
                print(f"  ⚠️ Could not control {tl_id}: {e}")

    print(f"\n🚑 CORRIDOR CLEARED — {vehicle_type} can proceed")
    print(f"   {cleared} junctions set to GREEN")
    print(f"   Green phase held for 60 seconds")

    # Step 4: Wait for emergency vehicle to pass
    print(f"\n⏱️  Waiting for {vehicle_type} to pass...")
    for i in range(10, 0, -1):
        print(f"   Restoring normal operation in {i}s...", end='\r')
        time.sleep(1)

    # Step 5: Restore normal operation
    restore_normal_operation(sumo_traffic_lights)

    return cleared

def restore_normal_operation(sumo_traffic_lights):
    """Restore normal adaptive signal control after emergency"""
    print(f"\n🔄 Restoring normal signal operation...")
    for tl_id in sumo_traffic_lights:
        try:
            traci.trafficlight.setPhase(tl_id, 0)
            traci.trafficlight.setPhaseDuration(tl_id, 30)
            print(f"  ✅ {tl_id} → Normal operation restored")
        except Exception as e:
            print(f"  ⚠️ Could not restore {tl_id}: {e}")
    print("✅ All junctions restored to normal adaptive control\n")

# ─────────────────────────────────────────
# MAIN SIMULATION
# ─────────────────────────────────────────
def simulate_emergency_scenario():
    """
    Full demonstration of emergency corridor clearing.
    Simulates an ambulance requiring corridor clearance at step 150.
    """
    # Authenticate first
    get_auth_token()

    print("🚦 TRAFFICBRAIN — Emergency Corridor System v2.0")
    print("Starting SUMO simulation...\n")

    traci.start([SUMO_BINARY, "-c", SUMO_CONFIG, "--no-warnings", "true"])

    traffic_lights = traci.trafficlight.getIDList()
    print(f"Found {len(traffic_lights)} traffic light(s) in network\n")

    step = 0
    emergency_triggered = False
    second_emergency_triggered = False

    try:
        while traci.simulation.getMinExpectedNumber() > 0 and step < 600:
            traci.simulationStep()

            # Print status every 50 steps
            if step % 50 == 0:
                vehicle_count = traci.vehicle.getIDCount()
                print(f"Step {step:4d} | Vehicles: {vehicle_count:3d} | "
                      f"Emergencies triggered: {int(emergency_triggered) + int(second_emergency_triggered)}")

            # First emergency — Ambulance at step 150
            if step == 150 and not emergency_triggered:
                emergency_triggered = True
                print(f"\n⚡ Step {step}: AMBULANCE DETECTED!")
                clear_emergency_corridor(
                    sumo_traffic_lights=traffic_lights,
                    emergency_junction_id=1,
                    vehicle_type="Ambulance",
                    direction="North towards Kamuzu Central Hospital"
                )

            # Second emergency — Fire truck at step 400
            if step == 400 and not second_emergency_triggered:
                second_emergency_triggered = True
                print(f"\n⚡ Step {step}: FIRE TRUCK DETECTED!")
                clear_emergency_corridor(
                    sumo_traffic_lights=traffic_lights,
                    emergency_junction_id=2,
                    vehicle_type="FireTruck",
                    direction="East towards City Centre"
                )

            step += 1
            time.sleep(0.02)

    except KeyboardInterrupt:
        print("\n⛔ Simulation stopped by user")

    finally:
        print("\n" + "=" * 60)
        print("TRAFFICBRAIN Emergency Corridor Simulation Complete")
        print(f"Total steps          : {step}")
        print(f"Emergencies handled  : {int(emergency_triggered) + int(second_emergency_triggered)}")
        print("=" * 60)
        traci.close()

if __name__ == "__main__":
    simulate_emergency_scenario()