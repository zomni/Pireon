"""Verificación F3: motor heurístico de coincidencias dispositivo-inventario.

Rematch retroactivo + scan-time sobre la snapshot más reciente y endpoints
de matching-summary / matches / rematch.

Uso: python tools/verify_matching_f3.py
"""

import re
import sys
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
assert r.status_code == 302, f"login failed: {r.status_code}"
print("login -> 302 /dashboard")


def get(path, expect=200):
    r = s.get(f"{BASE}{path}")
    if r.status_code != expect:
        print(f"  ! GET {path} -> {r.status_code} {r.text[:200]}")
        sys.exit(1)
    return r.json()


def post(path, expect=200):
    r = s.post(f"{BASE}{path}")
    if r.status_code != expect:
        print(f"  ! POST {path} -> {r.status_code} {r.text[:200]}")
        sys.exit(1)
    return r.json()


print("\n== Latest snapshot ==")
latest = get("/api/network-telemetry/latest?take=1")
snapshot_id = latest[0]["id"] if latest else None
assert snapshot_id, "no hay snapshots"
print("  snapshot:", snapshot_id, latest[0]["sourceName"], latest[0]["deviceCount"], "disp")

print("\n== matching-summary (antes del rematch) ==")
summary = get(f"/api/network-telemetry/snapshots/{snapshot_id}/matching-summary")
print("  found:", summary["found"], "| devices:", summary["deviceCount"],
      "| matched:", summary["matchedCount"], "| unmatched:", summary["unmatchedCount"],
      "| rate:", summary["matchRate"])
print("  keys:", summary["matchKeyCounts"])

print("\n== matches (pagina 1) ==")
page = get(f"/api/network-telemetry/snapshots/{snapshot_id}/matches?pageSize=5")
print("  total:", page["totalCount"], "| items:", len(page["items"]))
for item in page["items"][:5]:
    print("   -", item["deviceName"] or "(sin nombre)", "| ip:", item["ipAddress"],
          "| key:", item["matchKey"], "| matched:", item["matched"],
          "| item:", item["inventoryItemNumber"] or "-")

print("\n== rematch (POST) ==")
result = post(f"/api/network-telemetry/snapshots/{snapshot_id}/rematch")
print("  status:", result["status"], "| devices:", result["deviceCount"],
      "| matched:", result["matchedCount"], "| unmatched:", result["unmatchedCount"],
      "| changed:", result["changedCount"])
print("  keys:", result["matchKeyCounts"])

print("\n== matching-summary (después del rematch) ==")
summary = get(f"/api/network-telemetry/snapshots/{snapshot_id}/matching-summary")
print("  devices:", summary["deviceCount"],
      "| matched:", summary["matchedCount"], "| unmatched:", summary["unmatchedCount"],
      "| rate:", summary["matchRate"])
print("  keys:", summary["matchKeyCounts"])
print("  matchedByRisk:", summary["matchedByRiskLevel"])
print("  unmatchedByRisk:", summary["unmatchedByRiskLevel"])

print("\n== matches: sin coincidir, riesgo alto ==")
page = get(f"/api/network-telemetry/snapshots/{snapshot_id}/matches?matchState=unmatched&riskLevel=high&pageSize=3")
print("  total sin coincidir/alto:", page["totalCount"])
for item in page["items"][:3]:
    print("   -", item["deviceName"] or "(sin nombre)", "| risk:", item["riskLevel"], item["riskScore"])

print("\n== matches: coincididos por IP en nombre ==")
page = get(f"/api/network-telemetry/snapshots/{snapshot_id}/matches?matchKey=ip_in_name&pageSize=5")
print("  total ip_in_name:", page["totalCount"])
for item in page["items"][:5]:
    print("   -", item["deviceName"] or "(sin nombre)", "| ip:", item["ipAddress"],
          "| item:", item["inventoryItemNumber"], "| resp:", item["inventoryResponsibleUser"])

print("\nOK")
