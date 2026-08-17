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
print("login ->", r.status_code, r.headers.get("Location", ""))
assert r.status_code == 302 or r.status_code == 200

def check(label, ok, detail=""):
    print(f"[{'OK' if ok else 'FAIL'}] {label}" + (f" - {detail}" if detail else ""))
    return ok

ok = True

r = s.get(f"{BASE}/dashboard")
ok = check("GET /dashboard 200", r.status_code == 200, str(r.status_code)) and ok
ok = check("/dashboard tiene selector", 'organization-selector-dashboard' in r.text) and ok
ok = check("superadmin 'Todas' muestra badge TODOS", 'TODOS' in r.text and 'data-organization-label' in r.text) and ok

r = s.get(f"{BASE}/api/organizations")
orgs = r.json() if r.status_code == 200 else []
ok = check("GET /api/organizations 200", r.status_code == 200, str(r.status_code)) and ok
no_color = [o["name"] for o in orgs if not (o.get("Color") or o.get("color") or "").strip()]
ok = check("todas las orgs tienen Color", not no_color, f"sin color: {no_color}") and ok
for o in orgs:
    print("   org:", o["name"], "->", o.get("Color") or o.get("color"))

r = s.get(f"{BASE}/dashboard/network-telemetry")
ok = check("GET /dashboard/network-telemetry 200", r.status_code == 200, str(r.status_code)) and ok
ok = check("tabla escaneos con columna #", '<th>#</th>' in r.text) and ok
ok = check("card planificaciones presente (backend)", 'Planificaciones de captura' in r.text) and ok

r = s.get(f"{BASE}/admin/organizations")
ok = check("GET /admin/organizations 200", r.status_code == 200, str(r.status_code)) and ok
ok = check("swatch de color en indice orgs", 'background-color: #' in r.text) and ok

if orgs:
    org = orgs[0]
    r = s.get(f"{BASE}/admin/organizations/{org['id']}/edit")
    ok = check("GET edit org 200", r.status_code == 200, str(r.status_code)) and ok
    ok = check("input color en edit", 'name="Color"' in r.text) and ok

    r = s.get(f"{BASE}/dashboard?organizationId={org['id']}")
    ok = check(f"GET /dashboard?organizationId=... 200", r.status_code == 200, str(r.status_code)) and ok
    ok = check("org-seleccionada tiene badge con nombre", 'data-organization-label' in r.text and 'TODOS' not in r.text) and ok

r = s.get(f"{BASE}/api/network-telemetry/schedule")
items = r.json() if r.status_code == 200 else []
print("schedules:", len(items), [i.get("campusKey") for i in items])

r = s.get("http://localhost:8081/")
ok = check("SPA 8081 responde", r.status_code == 200, str(r.status_code)) and ok

print("RESULT:", "OK" if ok else "FAILED")
sys.exit(0 if ok else 1)
