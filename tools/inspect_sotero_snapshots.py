import sqlite3

c = sqlite3.connect(r"C:\Users\paolo.vilches\AppData\Local\Temp\opencode\sotero_live.db")
c.row_factory = sqlite3.Row

cols = [r[1] for r in c.execute("PRAGMA table_info(NetworkTelemetrySnapshots)")]
print("snapshot cols:", cols)
print("\n== snapshots (last 15 by ObservedAtUtc) ==")
rows = c.execute("SELECT * FROM NetworkTelemetrySnapshots ORDER BY ObservedAtUtc DESC LIMIT 15").fetchall()
for r in rows:
    print(dict(r))

print("\n== snapshot count by day (last 8 days) ==")
for r in c.execute("SELECT substr(ObservedAtUtc,1,10) d, COUNT(*) FROM NetworkTelemetrySnapshots GROUP BY d ORDER BY d DESC LIMIT 10"):
    print(r[0], r[1])

print("\n== scheduled scan runs (last 10) ==")
cols2 = [r[1] for r in c.execute("PRAGMA table_info(ScheduledScanRuns)")]
print("cols:", cols2)
for r in c.execute("SELECT * FROM ScheduledScanRuns ORDER BY ScheduledAtUtc DESC LIMIT 10"):
    print(dict(r))
