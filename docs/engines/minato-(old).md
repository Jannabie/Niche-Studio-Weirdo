# Minato Engine (Old / PAC)

**Tab Name:** Minato (Old)  
**Powered by:** Minato PAC Tools (bundled)  
**File Formats:** `.pac` (archives), `.bin` (scripts with SEG blocks)

---

## Supported Games

| Game | Notes |
|---|---|
| Maji de Watashi ni Koishinasai! (Majikoi) | Select correct Repack Mode for your version |
| Majikoi S | Select correct Repack Mode for your version |
| Other older Minato titles | `.pac` + `.bin` format |

---

## Overview

The older Minato engine stores game resources in `.pac` archives. Script data is stored in `.bin` files containing **SEG blocks** — the engine's internal segment format for dialogue and game logic. Repack Mode must match the specific game version being targeted.

This tab has **two independent sections**:
- **Archive** — unpack and repack `.pac` archives
- **Script Translation** — extract, translate, and repack `.bin` SEG scripts

---

## Section A — PAC Archive (Unpack & Repack)

### Step 1 — Select Input Archive

1. Under **ARCHIVE**, click **Browse** next to **INPUT .PAC FILE** and select the `.pac` archive from your game directory.

### Step 2 — Select Output Directory

1. Click **Browse** next to **OUTPUT DIRECTORY** and choose a folder where extracted files will be saved.

### Step 3 — Unpack PAC

1. Click **Unpack PAC**.
2. All files are extracted into the output directory.

### Step 4 — Repack PAC

1. After editing the extracted files, click **Browse** next to **INPUT FOLDER** (if not already set) and select the folder with your modified files.
2. Click **Repack PAC**.
3. A new `.pac` archive is produced.
4. Replace the original `.pac` in your game directory and test.

---

## Section B — Script Translation (.bin SEG Blocks)

### Step 5 — Load the Script File

1. Under **SCRIPT**, click **Browse** next to **INPUT .BIN FILE** and select a `.bin` script file from the extracted archive folder.

### Step 6 — Extract SEG

1. Click **Extract SEG**.
2. The SEG blocks are extracted and the dialogue strings are exported to an editable format.
3. Translate the text and save.

### Step 7 — Select Repack Mode

1. Choose the correct **Repack Mode** from the dropdown menu.

>  **The Repack Mode must match your target game version exactly.** Different Majikoi releases (Majikoi, Majikoi S, etc.) use different SEG block formats. Using the wrong mode will produce a broken script.

### Step 8 — Repack SEG

1. Click **Repack SEG**.
2. The tool writes your translated text back into the `.bin` script file in the correct SEG format.
3. Place the new `.bin` back into the extracted archive folder, then repack the `.pac` (Step 4).

---

## Full Workflow Summary

```
ARCHIVE WORKFLOW:
① Browse INPUT .PAC FILE
② Browse OUTPUT DIRECTORY
③ Click Unpack PAC → files extracted

SCRIPT WORKFLOW:
④ Browse INPUT .BIN FILE (from extracted folder)
⑤ Click Extract SEG → translate extracted text
⑥ Select correct REPACK MODE from dropdown (must match game version!)
⑦ Click Repack SEG → new .bin with translated text

FINAL:
⑧ Place new .bin back into extracted folder
⑨ Click Repack PAC → new .pac archive
⑩ Replace original .pac in game directory → test
```

