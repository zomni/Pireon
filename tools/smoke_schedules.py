import re
import requests

BASE = "http://localhost:5001"
USER = "admin@example.com"
PASS = "ChangeMe!123"

s = requests.Session()

r = s.get(f"{BASE}/Auth/Login", allow_redirects=True)
assert r.status_code == 200, "login page failed"
m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', r.text)
assert m, "antiforgery token not found"

r = s.post(
    f"{BASE}/Auth/Login",
    data={"__RequestVerificationToken": m.group(1), "Username": USER, "Password": PASS},
    allow_redirects=False,
)
print("login ->", r.status_code, r.headers.get("Location", ""))

def create(payload):
    r = s.post(f"{BASE}/api/network-telemetry/schedule", json=payload)
    print("create", payload["label"], "->", r.status_code)
    print("   ", r.text[:300])
    return r

created = create({
    "label": "Sotero Lun-Jue (3 turnos)",
    "cron": "0 30 8,13,17 * * 1-4",
    "timeZone": "America/Santiago",
    "campusKey": "sotero",
    "isEnabled": True,
    "sortOrder": 1,
})

created2 = create({
    "label": "Sotero Viernes (3 turnos)",
    "cron": "0 30 8,13,16 * * 5",
    "timeZone": "America/Santiago",
    "campusKey": "sotero",
    "isEnabled": True,
    "sortOrder": 2,
})

r = s.get(f"{BASE}/api/network-telemetry/schedule")
print("list ->", r.status_code)
items = r.json()
print("  items:", [(i.get("id"), i.get("label"), i.get("cron"), i.get("isValid"), i.get("nextOccurrenceLocal")) for i in items])

if items:
    first_id = items[0]["id"]
    r = s.put(f"{BASE}/api/network-telemetry/schedule/{first_id}", json={
        "label": "Sotero Lun-Jue (editado)",
        "cron": "0 45 9,14,18 * * 1-4",
        "timeZone": "America/Santiago",
        "campusKey": "sotero",
        "isEnabled": False,
        "sortOrder": 1,
    })
    print("update ->", r.status_code, r.text[:200])

    r = s.get(f"{BASE}/api/network-telemetry/schedule")
    items = r.json()
    print("  after update:", [(i.get("label"), i.get("cron"), i.get("isEnabled")) for i in items])

    r = s.delete(f"{BASE}/api/network-telemetry/schedule/{first_id}")
    print("delete ->", r.status_code)

    r = s.get(f"{BASE}/api/network-telemetry/schedule")
    items = r.json()
    print("  after delete:", [(i.get("label")) for i in items])

# delete leftover
for i in s.get(f"{BASE}/api/network-telemetry/schedule").json():
    s.delete(f"{BASE}/api/network-telemetry/schedule/{i['id']}")
print("cleanup done")

r = s.get(f"{BASE}/api/network-telemetry/scheduled-scans?page=1&pageSize=6&sortBy=scheduledAtUtc&sortDirection=desc")
print("scheduled-scans ->", r.status_code, "totalCount:", r.json().get("totalCount"))
