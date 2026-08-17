import re
import requests

BASE = "http://localhost:5001"
USER = "admin@example.com"
PASS = "ChangeMe!123"

s = requests.Session()
r = s.get(f"{BASE}/Auth/Login", allow_redirects=True)
m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', r.text)
assert m
r = s.post(
    f"{BASE}/Auth/Login",
    data={"__RequestVerificationToken": m.group(1), "Username": USER, "Password": PASS},
    allow_redirects=False,
)
print("login ->", r.status_code)

existing_orgs = s.get(f"{BASE}/api/organizations").json()
print("existing orgs:", [(o["name"], o["slug"]) for o in existing_orgs])

org = None
for o in existing_orgs:
    if o["slug"] == "sotero":
        org = o
        break

if org is None:
    r = s.post(
        f"{BASE}/api/organizations",
        json={"name": "Hospital Sótero del Río", "slug": "sotero", "contactEmail": "", "notes": "Importado desde sotero_map_api/sotero_map (legacy)."},
    )
    print("create org ->", r.status_code)
    if r.status_code not in (200, 201):
        print(r.text)
        raise SystemExit(1)
    org = r.json()

org_id = org["id"]
print("org id:", org_id)

sites = s.get(f"{BASE}/api/organizations/{org_id}/sites").json()
print("existing sites:", [x["campusKey"] for x in sites])

site = None
for x in sites:
    if x["campusKey"] == "sotero":
        site = x
        break

site_payload = {
    "campusKey": "sotero",
    "name": "Complejo Hospitalario Sótero del Río",
    "school": "cs",
    "centerLatitude": -33.576,
    "centerLongitude": -70.581,
    "zoom": 18,
    "boundsMinLatitude": -33.5801,
    "boundsMinLongitude": -70.5832,
    "boundsMaxLatitude": -33.5720,
    "boundsMaxLongitude": -70.5763,
    "floors": ["-1", "0", "1", "2", "3", "4", "5"],
    "defaultFloor": "b1",
}

if site is None:
    r = s.post(f"{BASE}/api/organizations/{org_id}/sites", json=site_payload)
    print("create site ->", r.status_code)
    if r.status_code not in (200, 201):
        print(r.text)
        raise SystemExit(1)
    site = r.json()
else:
    r = s.put(f"{BASE}/api/organizations/{org_id}/sites/{site['id']}", json=site_payload)
    print("update site ->", r.status_code, r.text[:120])

print("site:", site)

# verify via session
r = s.get(f"{BASE}/api/auth/session")
print("session sites:", [(x["campusKey"], x["school"], x["floors"], x["defaultFloor"]) for x in r.json()["sites"]])
