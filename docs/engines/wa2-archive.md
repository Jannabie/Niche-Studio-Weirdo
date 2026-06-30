# Leaf Engine (WA2 Archive / KCAP)

> Unpack and repack `.pak` KCAP archives with Shift-JIS filename preservation.

**Example games:** White Album 2, other Leaf/Aquaplus titles

---

> ⚠️ **Warning:** The workspace directory must contain **only files from the `.pak` archive**. Repacking foreign binaries (e.g. tools, executables) alongside game files **will cause game crashes**.

---

## Tools Available

### PAK Archive (`.pak` KCAP)

| Button | Action |
|---|---|
| **Unpack .pak** | Extract all files from the archive into the workspace folder |
| **Repack to .pak** | Pack the workspace folder back into a new `.pak` archive |

---

## How to Use

**Before you start:**
- Create a dedicated, **empty** folder to use as your workspace
- Never reuse a folder that contains other tools or executables

**Step 1 — Unpack:**
1. Browse → select your **Isolated Workspace Directory** (the empty folder you created)
2. Browse → select the target `.pak` file from the game
3. Click **Unpack .pak** — files are extracted into the workspace

**Step 2 — Translate:**
- Edit the extracted script files in the workspace folder
- Filenames are preserved in Shift-JIS encoding automatically

**Step 3 — Repack:**
1. Make sure the workspace contains **only** the game files (no extra tools)
2. Click **Repack to .pak** → save the output `.pak`
3. Replace the original `.pak` in the game directory
