# Buriko / BGI Engine

**Tab Name:** Buriko  
**Powered by:** Buriko/BGI Tools (bundled)  
**File Formats:** `.arc` (archives), `.sc` (scripts)

---

## Supported Games

| Game | Notes |
|---|---|
| Higurashi no Naku Koro ni | `.arc` archives |
| Umineko no Naku Koro ni | `.arc` archives |
| Sakura no Uta | `.arc` archives, `.sc` scripts |
| Subarashiki Hibi ~Furenzoku Sonzai~ | `.arc` archives, `.sc` scripts |
| Other BGI engine titles | — |

---

## Overview

The Buriko/BGI engine stores all game resources — graphics, audio, and scripts — inside `.arc` archives. Dialogue scripts are stored in compiled `.sc` (script command) files inside those archives.

This tab has **two independent sections**:
- **Archive** — unpack and repack `.arc` files
- **Script Translation** — extract, translate, and inject `.sc` files

---

## Section A — ARC Archive (Unpack & Repack)

### Step 1 — Select the Archive

1. Under **ARCHIVE**, click **Browse** next to **INPUT .ARC FILE** and select the `.arc` archive from your game directory.

### Step 2 — Select Output Folder

1. Click **Browse** next to **OUTPUT FOLDER** and choose an empty folder where extracted files will be saved.

### Step 3 — Unpack

1. Click **Unpack .ARC**.
2. All resources are extracted into the output folder.

### Step 4 — Repack

1. After modifying the extracted files (e.g. replacing translated `.sc` files), click **Browse** next to **INPUT FOLDER** and select the folder.
2. Click **Repack Folder → .ARC**.
3. A Save dialog will appear — the output file will be automatically named with a `_new` suffix (e.g. `script_new.arc`).
4. Replace the original `.arc` in your game directory with the new file and test.

> 💡 **The repacked archive is always named `*_new.arc`** via the Save dialog. Rename it to match the original archive name before placing it in your game directory.

---

## Section B — Script Translation (.sc ↔ JSON)

### Step 5 — Load the Script File

1. Under **SCRIPT**, click **Browse** next to **INPUT .SC FILE** and select a `.sc` script file from the extracted archive folder.

### Step 6 — Parse to JSON

1. Click **Parse .SC → JSON**.
2. A `.json` file is created next to the `.sc` containing all dialogue strings.
3. Open the JSON in a text editor (Notepad++ recommended), translate the strings, and save.

### Step 7 — Inject Translation

1. After translating, click **Inject JSON → .SC**.
2. The tool writes your translated strings back into a new `.sc` file.
3. Place the new `.sc` file into your extracted archive folder and repack (Section A, Step 4).

---

## Full Workflow Summary

```
ARCHIVE WORKFLOW:
① Browse INPUT .ARC FILE
② Browse OUTPUT FOLDER
③ Click Unpack .ARC → files extracted to output folder

SCRIPT WORKFLOW:
① Browse the .sc file from the extracted folder
② Click Parse .SC → JSON → translate the JSON strings
③ Click Inject JSON → .SC → new translated .sc file

FINAL STEPS:
① Replace the original .sc files in the extracted folder with translated ones
② Click Repack Folder → .ARC → save as *_new.arc
③ Rename *_new.arc to match original → place in game directory → test
```

> ⚠️ **Always work on a copy** of the extracted folder. Keep the originals untouched so you can re-extract cleanly if needed.
