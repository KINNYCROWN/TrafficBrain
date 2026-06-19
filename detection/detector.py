import cv2
import requests
import time
import math
from ultralytics import YOLO
from datetime import datetime

# ─────────────────────────────────────────
# CONFIGURATION
# ─────────────────────────────────────────
API_URL = "http://localhost:5275"
JUNCTION_ID = 1
SEND_INTERVAL = 3
CONFIDENCE_THRESHOLD = 0.4
AUTH_TOKEN = None

# Vehicle classes from COCO dataset
VEHICLE_CLASSES = {
    2:  {"name": "Car",        "color": (0, 255, 0)},
    3:  {"name": "Motorcycle", "color": (0, 165, 255)},
    5:  {"name": "Bus",        "color": (255, 0, 0)},
    7:  {"name": "Truck",      "color": (0, 0, 255)},
    0:  {"name": "Pedestrian", "color": (255, 255, 0)},
}

# ─────────────────────────────────────────
# AUTHENTICATION
# ─────────────────────────────────────────
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
            return True
        else:
            print(f"⚠️ Auth failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Auth error: {e}")
        return False

# ─────────────────────────────────────────
# LOAD MODEL
# ─────────────────────────────────────────
print("Loading YOLOv8 model...")
model = YOLO('yolov8n.pt')
print("Model loaded\n")

# ─────────────────────────────────────────
# SPEED ESTIMATION
# ─────────────────────────────────────────
previous_positions = {}

def estimate_speed(track_id, current_center, fps=30):
    if track_id in previous_positions:
        prev_center = previous_positions[track_id]
        pixel_distance = math.sqrt(
            (current_center[0] - prev_center[0]) ** 2 +
            (current_center[1] - prev_center[1]) ** 2
        )
        speed_ms = pixel_distance * 0.05 * fps
        speed_kmh = speed_ms * 3.6
        previous_positions[track_id] = current_center
        return round(min(speed_kmh, 120), 1)
    else:
        previous_positions[track_id] = current_center
        return 0

# ─────────────────────────────────────────
# CONGESTION SCORING
# ─────────────────────────────────────────
def calculate_congestion_score(total_vehicles, avg_speed):
    vehicle_score = min(total_vehicles * 4, 60)
    speed_score = max(0, 40 - avg_speed * 0.8)
    return min(int(vehicle_score + speed_score), 100)

def get_congestion_label(score):
    if score < 25:  return ("FREE FLOW",  (0, 255, 0))
    if score < 50:  return ("MODERATE",   (0, 165, 255))
    if score < 75:  return ("HEAVY",      (0, 69, 255))
    return              ("GRIDLOCK",  (0, 0, 255))

# ─────────────────────────────────────────
# API COMMUNICATION
# ─────────────────────────────────────────
def send_to_api(cars, motorcycles, buses, trucks, pedestrians, speeds, congestion_score):
    """Send detection data to TrafficBrain API with authentication"""
    global AUTH_TOKEN

    if not AUTH_TOKEN:
        if not get_auth_token():
            print("❌ Cannot send data — not authenticated")
            return

    total = cars + motorcycles + buses + trucks
    avg_speed = round(sum(speeds) / len(speeds), 1) if speeds else 0

    try:
        response = requests.post(
            f"{API_URL}/api/traffic/vehiclecount",
            json={
                "junctionId": JUNCTION_ID,
                "cars": cars,
                "motorcycles": motorcycles,
                "buses": buses,
                "trucks": trucks,
                "pedestrians": pedestrians,
                "totalVehicles": total,
                "averageSpeed": avg_speed,
                "congestionScore": congestion_score,
                "lane": "Main",
                "recordedAt": datetime.utcnow().isoformat() + "Z"
            },
            headers={
                "Content-Type": "application/json",
                "Authorization": f"Bearer {AUTH_TOKEN}"
            },
            timeout=3
        )
        if response.status_code == 200:
            print(f"[{datetime.now().strftime('%H:%M:%S')}] ✅ Sent — "
                  f"Cars:{cars} Bikes:{motorcycles} Buses:{buses} "
                  f"Trucks:{trucks} Peds:{pedestrians} "
                  f"Speed:{avg_speed}km/h Congestion:{congestion_score}/100")
        elif response.status_code == 401:
            # Token expired — re-authenticate
            print("⚠️ Token expired — re-authenticating...")
            AUTH_TOKEN = None
            get_auth_token()
        else:
            print(f"[{datetime.now().strftime('%H:%M:%S')}] ⚠️ API {response.status_code}")
    except Exception as e:
        print(f"[{datetime.now().strftime('%H:%M:%S')}] ❌ {e}")

# ─────────────────────────────────────────
# MAIN DETECTION LOOP
# ─────────────────────────────────────────
def detect_vehicles(source=0):
    # Authenticate first
    get_auth_token()

    cap = cv2.VideoCapture(source)
    fps = cap.get(cv2.CAP_PROP_FPS) or 30

    print("=" * 55)
    print("  TRAFFICBRAIN — Vehicle Detection Engine v2.0")
    print(f"  Junction ID : {JUNCTION_ID}")
    print(f"  API URL     : {API_URL}")
    print(f"  Send every  : {SEND_INTERVAL} seconds")
    print("  Press Q to quit")
    print("=" * 55 + "\n")

    last_send_time = time.time()
    frame_speeds = []

    while True:
        ret, frame = cap.read()
        if not ret:
            break

        results = model.track(frame, persist=True, verbose=False)

        cars = motorcycles = buses = trucks = pedestrians = 0
        frame_speeds = []

        for result in results:
            if result.boxes is None:
                continue
            for box in result.boxes:
                class_id = int(box.cls[0])
                confidence = float(box.conf[0])

                if class_id not in VEHICLE_CLASSES or confidence < CONFIDENCE_THRESHOLD:
                    continue

                vehicle_info = VEHICLE_CLASSES[class_id]
                vehicle_name = vehicle_info["name"]
                color = vehicle_info["color"]

                if class_id == 2:   cars += 1
                elif class_id == 3: motorcycles += 1
                elif class_id == 5: buses += 1
                elif class_id == 7: trucks += 1
                elif class_id == 0: pedestrians += 1

                x1, y1, x2, y2 = map(int, box.xyxy[0])
                cx, cy = (x1 + x2) // 2, (y1 + y2) // 2

                track_id = int(box.id[0]) if box.id is not None else -1
                speed = 0
                if track_id >= 0:
                    speed = estimate_speed(track_id, (cx, cy), fps)
                    if speed > 0:
                        frame_speeds.append(speed)

                cv2.rectangle(frame, (x1, y1), (x2, y2), color, 2)
                label = f"{vehicle_name} {confidence:.2f}"
                if speed > 0:
                    label += f" {speed}km/h"
                cv2.putText(frame, label, (x1, y1 - 8),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.55, color, 2)
                cv2.circle(frame, (cx, cy), 4, color, -1)

        total = cars + motorcycles + buses + trucks
        avg_speed = sum(frame_speeds) / len(frame_speeds) if frame_speeds else 0
        congestion_score = calculate_congestion_score(total, avg_speed)
        congestion_label, congestion_color = get_congestion_label(congestion_score)

        # HUD overlay
        overlay = frame.copy()
        cv2.rectangle(overlay, (0, 0), (260, 230), (0, 0, 0), -1)
        cv2.addWeighted(overlay, 0.5, frame, 0.5, 0, frame)

        y = 25
        cv2.putText(frame, "TRAFFICBRAIN LIVE", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 0), 2)
        y += 28
        cv2.putText(frame, f"Junction: {JUNCTION_ID}", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (200, 200, 200), 1)
        y += 25
        cv2.putText(frame, f"Cars      : {cars}", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 1)
        y += 22
        cv2.putText(frame, f"Motorcycles: {motorcycles}", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 165, 255), 1)
        y += 22
        cv2.putText(frame, f"Buses     : {buses}", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 0, 0), 1)
        y += 22
        cv2.putText(frame, f"Trucks    : {trucks}", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 1)
        y += 22
        cv2.putText(frame, f"Pedestrians: {pedestrians}", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 0), 1)
        y += 28
        cv2.putText(frame, f"CONGESTION: {congestion_label} ({congestion_score}/100)", (10, y),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.55, congestion_color, 2)

        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        cv2.putText(frame, timestamp,
                    (frame.shape[1] - 200, frame.shape[0] - 10),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.45, (200, 200, 200), 1)

        if time.time() - last_send_time >= SEND_INTERVAL:
            send_to_api(cars, motorcycles, buses, trucks,
                        pedestrians, frame_speeds, congestion_score)
            last_send_time = time.time()

        cv2.imshow("TRAFFICBRAIN — Vehicle Detection", frame)

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    detect_vehicles(source=0)