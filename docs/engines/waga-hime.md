# Minato New Engine (Waga Hime / ACV1)

> Extract and repack ACV1 `.dat` archives and parse scripts to JSON/CSV.

**Example games:** Wagamama High Spec, and newer Minato Soft titles

---

## Settings

Before doing anything, configure the encryption keys and compression options:

| Setting | Default | Description |
|---|---|---|
| **Master Hex Key** | `0x8B6A4E5F` | Master decryption key for the archive |
| **Script Hex Key** | `0x3793B711` | Key used specifically for script files |
| **Zlib Compression Level** | `9` (max) | Compression level when repacking (0 = none, 9 = max) |
| **Skip raw** | off | Skip raw binary files during processing |
| **Skip text** | off | Skip text files during processing |

> **Note:** The default keys work for most Minato New engine games. Only change them if you have game-specific key information.

---

## Tools Available

### Archive Tools (`.dat` ACV1)

| Button | Action |
|---|---|
| **Unpack .dat** | Extract all files from the ACV1 archive |
| **Repack .dat** | Pack a folder back into an ACV1 `.dat` archive |

**How to use:**
1. Browse → select your target `.dat` file
2. Browse → select output folder (for unpack) or source folder (for repack)
3. Click **Unpack .dat** or **Repack .dat**

---

### Script Tools

| Button | Action |
|---|---|
| **Parse → JSON** | Extract script text into a JSON file |
| **Parse → CSV** | Extract script text into a CSV spreadsheet |
| **Inject → Script** | Inject translated JSON/CSV back into the script |

**How to use:**

**Step 1 — Parse:**
1. Browse → select your script file (extracted from the `.dat` archive)
2. Click **Parse → JSON** or **Parse → CSV** depending on your preference
3. Save the output file

**Step 2 — Translate:**
- Edit the JSON or CSV with your translations
- CSV format is useful for spreadsheet-based workflows (Google Sheets, Excel)

**Step 3 — Inject:**
1. Browse → select the original script and your translated file
2. Click **Inject → Script** → save the new script
3. Repack the folder back into `.dat` using the archive tool above

---

## Full Workflow

```
Unpack .dat → Parse script → Translate JSON/CSV → Inject → Repack .dat
```
