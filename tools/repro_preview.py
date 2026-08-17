import requests

BASE = "http://localhost:5001"
USER = "admin@example.com"
PASS = "ChangeMe!123"

s = requests.Session()
import re
r = s.get(f"{BASE}/Auth/Login", allow_redirects=True)
m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', r.text)
s.post(
    f"{BASE}/Auth/Login",
    data={"__RequestVerificationToken": m.group(1), "Username": USER, "Password": PASS},
    allow_redirects=False,
)

r = s.post(
    f"{BASE}/api/network-telemetry/schedule/preview",
    json={"cron": "30 08 * * *", "timeZone": "America/Santiago", "count": 3},
)
print(r.status_code)
print(r.text)
