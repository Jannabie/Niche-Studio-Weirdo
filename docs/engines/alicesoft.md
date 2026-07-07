# AliceSoft Engine

**Tab Name:** AliceSoft  
**Powered by:** AliceSoft SDK Tools (bundled)  
**File Formats:** `.ain` (scripts), `.afa` / `.ald` (archives), `.cg` (images), `.ex` (databases)

---

## Supported Games

| Game | Notes |
|---|---|
| Rance series | Various titles; use correct game version |
| Evenicle | `.afa` archives, `.ain` scripts |
| Other AliceSoft titles | `.ald` archives on older games |

---

## Overview

The AliceSoft engine is a long-running proprietary engine used across many of AliceSoft's titles. It has four distinct resource layers, each handled independently:

- **AIN** — compiled game scripts (bytecode)
- **EX** — game databases (items, flags, variables)
- **AFA / ALD** — resource archives (CG, sound, data)
- **CG** — proprietary image format

This tab handles all four layers.

> If you look string name guide go to: **[Rance String Name Guide ](https://github.com/Jannabie/Niche-Studio-Weirdo/blob/main/Rance%20Guide/Rance%20Name%20Guide.md)**

---

## Section A — AIN Script (Dump & Rebuild)

### Step 1 — Dump .AIN → Text

1. Under **AIN SCRIPT**, click **Browse** next to **INPUT .AIN** and select the compiled script file (e.g. `System40.ain`, `AliceStart.ain`).
2. Click **Dump AIN**.
3. A human-readable text dump is saved next to the `.ain` file. Open and translate the dialogue strings.

### Step 2 — Rebuild .AIN from Text

1. After translating, click **Rebuild AIN**.
2. The tool recompiles the text dump back into a valid `.ain` binary.
3. Replace the original `.ain` in your game directory with the rebuilt file and test.

>  **Do not alter the structure of the AIN dump.** Only change the content of dialogue/string fields. Modifying opcodes or metadata will corrupt the script.

---

## Section B — EX Database (Dump & Rebuild)

### Step 3 — Dump .EX → Text

1. Under **EX DATABASE**, click **Browse** next to **INPUT .EX** and select the database file.
2. Click **Dump EX**.
3. A text export is generated. Translate any localizable strings (item names, descriptions, etc.).

### Step 4 — Rebuild .EX from Text

1. Click **Rebuild EX** to recompile the translated text back into a `.ex` binary.
2. Replace the original `.ex` in your game directory.

---

## Section C — AFA / ALD Archive (Unpack & Repack)

### Step 5 — Unpack Archive

1. Under **AFA / ALD ARCHIVE**, click **Browse** next to **INPUT ARCHIVE** and select the `.afa` or `.ald` file.
2. Click **Browse** next to **OUTPUT FOLDER** and choose an empty destination folder.
3. Click **Unpack**. All resources are extracted to the output folder.

### Step 6 — Repack Archive

1. After modifying the extracted files, click **Browse** next to **INPUT FOLDER** and select the folder containing your modified files.
2. Click **Repack**. A new `.afa` or `.ald` archive is produced.
3. Replace the original archive in your game directory.

---

## Section D — CG Image (Convert & Rebuild)

### Step 7 — CG → PNG

1. Under **CG IMAGE**, click **Browse** next to **INPUT .CG** and select an image file.
2. Click **CG → PNG**. A `.png` is saved next to the original.
3. Edit the image in your preferred image editor.

### Step 8 — PNG → CG

1. Click **Browse** next to **MODIFIED .PNG** and select your edited image.
2. Click **PNG → CG** to convert back to the proprietary format.
3. Replace the original `.cg` in your extracted archive folder, then repack.

---

## Full Workflow Summary

```
AIN SCRIPT WORKFLOW:
① Dump AIN → text file
② Translate dialogue strings
③ Rebuild AIN → new .ain binary
④ Replace .ain in game directory → test

EX DATABASE WORKFLOW:
① Dump EX → text file
② Translate item/flag strings
③ Rebuild EX → new .ex binary
④ Replace .ex in game directory → test

ARCHIVE WORKFLOW:
① Unpack AFA/ALD → output folder
② Modify files inside (translate CG, replace audio, etc.)
③ Repack → new archive
④ Replace original archive in game directory → test

CG IMAGE WORKFLOW:
① CG → PNG (convert for editing)
② Edit the PNG in your image editor
③ PNG → CG (convert back)
④ Place new .cg into extracted folder, then repack archive
```

