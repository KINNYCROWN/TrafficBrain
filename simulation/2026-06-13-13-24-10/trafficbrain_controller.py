import traci
import time
import requests
from datetime import datetime

# ─────────────────────────────────────────
# CONFIGURATION
# ─────────────────────────────────────────
SUMO_CONFIG = "osm.sumocfg"
SUMO_BINARY = "sumo-gui"
API_URL = "http://localhost:5275"
MAX_STEPS = 1000
AUTH_TOKEN = None

def get_auth_token():
    """Login and get JWT token"""
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
        else:
            print(f"⚠️ Auth failed: {response.status_code}")
    except Exception as e:
        print(f"❌ Auth error: {e}")

def get_traffic_light_ids():
    return traci.trafficlight.getIDList()

def get_junction_queue(tl_id):
    total_waiting = 0
    try:
        controlled_lanes = traci.trafficlight.getControlledLanes(tl_id)
        for lane in set(controlled_lanes):
            total_waiting += traci.lane.getLastStepHaltingNumber(lane)
    except Exception:
        pass
    return total_waiting

def get_avg_waiting_time(tl_id):
    total_wait = 0
    count = 0
    try:
        controlled_lanes = traci.trafficlight.getControlledLanes(tl_id)
        for lane in set(controlled_lanes):
            vehicle_ids = traci.lane.getLastStepVehicleIDs(lane)
            for v in vehicle_ids:
                total_wait += traci.vehicle.getWaitingTime(v)
                count += 1
    except Exception:
        pass
    return (total_wait / count) if count > 0 else 0

def adaptive_signal_control(tl_id):
    try:
        queue = get_junction_queue(tl_id)
        if queue > 8:
            traci.trafficlight.setPhaseDuration(tl_id, 25)
        elif queue < 2:
            traci.trafficlight.setPhaseDuration(tl_id, 8)
        return queue
    except Exception:
        return 0

def send_performance_metric(junction_id, control_mode, avg_wait, total_vehicles, congestion_score):
    """Send performance data to TrafficBrain API with authentication"""
    global AUTH_TOKEN
    if not AUTH_TOKEN:
        get_auth_token()
    try:
        response = requests.post(
            f"{API_URL}/api/analytics/performance",
            json={
                "junctionId": junction_id,
                "controlMode": control_mode,
                "averageWaitTime": round(avg_wait, 2),
                "maxWaitTime": round(avg_wait * 1.5, 2),
                "totalVehiclesProcessed": total_vehicles,
                "throughputPerHour": total_vehicles * 3,
                "fuelWasteEstimateLitres": round(avg_wait * 0.05, 3),
                "co2SavedKg": round(avg_wait * 0.12, 3) if control_mode == "Adaptive" else 0,
                "congestionScore": congestion_score
            },
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {AUTH_TOKEN}"
            },
            timeout=5
        )
        if response.status_code == 200:
            print(f"✅ Performance metric saved successfully")
        else:
            print(f"⚠️ API response: {response.status_code} — {response.text}")
    except Exception as e:
        print(f"❌ Failed to send: {e}")

def run_simulation(control_mode="Adaptive"):
    """
    Main simulation loop.
    control_mode: Adaptive (AI) or Fixed (default timers)
    """
    # Authenticate first
    get_auth_token()

    traci.start([SUMO_BINARY, "-c", SUMO_CONFIG, "--no-warnings", "true"])

    print("=" * 60)
    print(f"  TRAFFICBRAIN AI CONTROLLER — Mode: {control_mode}")
    print("=" * 60)

    traffic_lights = get_traffic_light_ids()
    print(f"Found {len(traffic_lights)} traffic light(s)\n")

    step = 0
    total_wait_accum = 0
    wait_samples = 0
    total_vehicles_seen = set()

    try:
        while traci.simulation.getMinExpectedNumber() > 0 and step < MAX_STEPS:
            traci.simulationStep()

            for v in traci.vehicle.getIDList():
                total_vehicles_seen.add(v)

            if step % 20 == 0:
                step_waits = []
                step_queue = 0

                for tl_id in traffic_lights[:10]:
                    if control_mode == "Adaptive":
                        queue = adaptive_signal_control(tl_id)
                    else:
                        queue = get_junction_queue(tl_id)

                    wait = get_avg_waiting_time(tl_id)
                    step_waits.append(wait)
                    step_queue += queue

                avg_wait = sum(step_waits) / len(step_waits) if step_waits else 0
                total_wait_accum += avg_wait
                wait_samples += 1

                congestion_score = min(int(step_queue * 2), 100)

                print(f"Step {step:4d} | Vehicles: {len(traci.vehicle.getIDList()):3d} | "
                      f"Avg Wait: {avg_wait:5.1f}s | Queue: {step_queue:3d} | "
                      f"Congestion: {congestion_score}/100")

            step += 1

    except KeyboardInterrupt:
        print("\nSimulation stopped by user")

    finally:
        overall_avg_wait = (total_wait_accum / wait_samples) if wait_samples > 0 else 0
        total_processed = len(total_vehicles_seen)

        print("\n" + "=" * 60)
        print(f"SIMULATION COMPLETE — Mode: {control_mode}")
        print(f"Total steps: {step}")
        print(f"Total vehicles processed: {total_processed}")
        print(f"Overall average wait time: {overall_avg_wait:.2f}s")
        print("=" * 60)

        congestion_score = min(int(overall_avg_wait), 100)
        send_performance_metric(
            junction_id=1,
            control_mode=control_mode,
            avg_wait=overall_avg_wait,
            total_vehicles=total_processed,
            congestion_score=congestion_score
        )

        traci.close()

if __name__ == "__main__":
    import sys
    mode = sys.argv[1] if len(sys.argv) > 1 else "Adaptive"
    run_simulation(control_mode=mode)