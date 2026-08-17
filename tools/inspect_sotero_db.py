import sqlite3

c = sqlite3.connect(r"C:\Users\paolo.vilches\AppData\Local\Temp\opencode\sotero_live.db")
tables = [r[0] for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")]
print("tables:", tables)
for t in tables:
    try:
        count = c.execute(f"SELECT COUNT(*) FROM [{t}]").fetchone()[0]
    except Exception as e:
        count = f"ERR {e}"
    print(f"  {t}: {count} rows")
