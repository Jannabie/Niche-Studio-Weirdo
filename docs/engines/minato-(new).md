# Minato Engine (New / ACV1)

**Tab Name:** Minato (New)  
**Powered by:** Minato ACV1 Tools (bundled)  
**File Formats:** `.dat` (ACV1 encrypted archives)

---

## Supported Games

| Game | Notes |
|---|---|
| Waga Himegimi ni Eikan o | ACV1 `.dat` format |
| Wagamama High Spec | ACV1 `.dat` format |

---

## Overview

The Minato (New/ACV1) engine stores all game data in encrypted `.dat` archives using a two-key encryption scheme: a **Master Hex Key** for the archive layer and a **Script Hex Key** for the script layer. This tab handles both archive unpacking/repacking and script parsing/injection.

---

## Step 1 — Configure Encryption Keys & Compression

Before performing any operations, set the correct encryption parameters:

| Setting | Default | Notes |
|---|---|---|
| **Master Hex Key** | `0x8B6A4E5F` | Archive-level decryption key |
| **Script Hex Key** | `0x3793B711` | Script-level decryption key |
| **Zlib Compression Level** | `6` (0–9) | 0 = no compression, 9 = maximum |

>  **Using the wrong key will produce a garbled or empty extraction.** If the defaults do not work for your specific game build, check your game's documentation or community resources for the correct keys.

---

## Step 2 — Unpack .DAT Archive

1. Click **Browse** next to **INPUT .DAT FILE** and select the `.dat` archive.
2. Click **Unpack .dat**.
3. Files are extracted to a folder next to the `.dat`, decrypted using the configured Master Hex Key.

---

## Step 3 — Repack .DAT Archive

1. After translating all script files, click **Browse** next to **INPUT FOLDER** and select the folder containing your modified files.
2. Click **Repack .dat**.
3. A new encrypted `.dat` archive is produced using the configured keys and compression level.
4. Replace the original `.dat` in your game directory and test.

---

## Section B — Script Translation

The Minato (New) engine supports two export formats for translation work:

### Step 4 — Parse → JSON

1. Click **Browse** next to **INPUT SCRIPT FILE** and select a script file from the unpacked folder.
2. Click **Parse → JSON**.
3. A JSON file is generated. Open it, translate the dialogue strings, and save.

### Step 4 (Alternative) — Parse → CSV

1. Click **Parse → CSV** if you prefer working in spreadsheet format.
2. Open the `.csv` in Excel or LibreOffice Calc, translate, and save.

### Step 5 — Inject → Script

1. After translating (JSON or CSV), click **Inject → Script**.
2. The tool writes your translations back into the script file.
3. Place the modified script file back into the unpacked folder before repacking.

---

## Full Workflow Summary

```
① Set Master Hex Key (default: 0x8B6A4E5F)
② Set Script Hex Key (default: 0x3793B711)
③ Set Zlib Compression Level (default: 6)
④ Browse INPUT .DAT FILE → Click Unpack .dat → files extracted

SCRIPT:
⑤ Browse script file → Click Parse → JSON (or CSV)
⑥ Translate the JSON/CSV strings
⑦ Click Inject → Script → translated script file generated

FINAL:
⑧ Place translated scripts back into extracted folder
⑨ Click Repack .dat → new encrypted archive
⑩ Replace original .dat in game directory → test
```

