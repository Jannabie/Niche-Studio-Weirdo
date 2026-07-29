# Leaf / KCAP Engine

**Tab Name:** Leaf  
**Powered by:** Leaf KCAP Tools (bundled) + Native C# repacker  
**File Formats:** `.pak` (KCAP archives), `.txt` (CSV script dumps)

---

## Supported Games

| Game | Notes |
|---|---|
| White Album 2 | Shift-JIS filenames inside `.pak`, BNR CSV script format |

---

## Overview

The Leaf engine uses KCAP-format `.pak` archives to store all game resources including scripts. Filenames inside the archive are encoded in Shift-JIS. The tool correctly handles Shift-JIS filename preservation during extraction and repacking.

The tab is split into two independent tools:

- **PAK Archive Tool** — Unpack and repack `.pak` (KCAP) archives
- **TXT Script Parser Tool** — Convert the raw BNR CSV script dump to a readable vertical format for translation, and inject it back

> **CRITICAL — Clean Workspace Required Before Repacking:** Any extra files present in the workspace folder at repack time will be bundled into the new archive. Always start with a clean, empty workspace folder — or verify that it contains only the files you intend to include.

---

## PAK Archive — Step by Step

### Step 1 — Select Workspace Directory

1. Click **Browse** next to **ISOLATED WORKSPACE DIRECTORY** and select an **empty** folder.

> The workspace must be empty before repacking. Leftover files from a previous session will be accidentally included in your new archive.

### Step 2 — Select the PAK Archive

1. Click **Browse** next to **TARGET .PAK FILE** and select the `.pak` file from your game directory.

### Step 3 — Unpack .PAK

1. Click **Unpack .pak**.
2. All files are extracted into the workspace directory, preserving Shift-JIS filenames correctly.
3. Make your translation edits to the extracted files and save.

> **Shift-JIS filenames** will display correctly if your system has Japanese locale set. If filenames appear as question marks or boxes, set your system locale to Japanese (or use a Unicode-aware file manager) before working with them.

### Step 4 — Repack to .PAK

1. Ensure the workspace directory contains **only** the files you want in the final archive.
2. Click **Repack to .pak**.
3. A new `.pak` archive (`repacked_*.pak`) is generated from the workspace contents alongside the workspace folder.
4. Replace the original `.pak` in your game directory with the new file and test.

---

## TXT Script Parser — Step by Step

The Leaf script format stores all dialogue as a flat comma-separated `.txt` file (BNR CSV dump). Raw commas inside line entries use `~` as an escape character inside the game engine.

### Step 1 — Parse CSV to TXT

1. Obtain the raw CSV `.txt` file by dumping it from the BNR tool.
2. Click **Browse** next to **PARSE: SELECT RAW CSV .TXT FILE** and select your raw file.
3. Click **Parse**.
4. A new file `<name>_parsed.txt` is generated in the same folder. Each entry is separated by an index comment `// [0000]` for easy navigation.

> You can now open `_parsed.txt` in any text editor and translate line by line.

### Step 2 — Inject Translation Back

1. Click **Browse** next to **INJECT: SELECT TRANSLATED _PARSED.TXT FILE** and select your completed `_parsed.txt` translation file.
2. Click **Back to Format (Inject)**.
3. A new file `<name>_repacked.txt` is generated — this is the re-encoded CSV ready for the BNR repacker.

> **Comma handling:** You can freely use regular commas `,` in your translation. The injector automatically converts them to `~` to prevent breaking the game's CSV array structure.

---

## Full Workflow Summary

```
[ARCHIVE]
① Browse ISOLATED WORKSPACE DIRECTORY → choose an empty folder
② Browse TARGET .PAK FILE
③ Click Unpack .pak → files extracted (Shift-JIS names preserved)
④ Translate/edit the extracted files
⑤ Verify workspace contains ONLY the intended files
⑥ Click Repack to .pak → repacked_*.pak generated
⑦ Replace original .pak in game directory → test

[SCRIPT]
① Dump raw CSV .txt from BNR tool
② Browse raw CSV .txt → Click Parse → _parsed.txt generated
③ Translate _parsed.txt line by line
④ Browse _parsed.txt → Click Back to Format (Inject) → _repacked.txt generated
⑤ Feed _repacked.txt into BNR repacker → test in-game
```
