# Abogado SDK (DSK Archive)

**Tab Name:** Abogado (DSK)  
**Powered by:** Abogado SDK Tools (bundled)  
**File Formats:** `.pft` (key file), `.dsk` (archive), `.scf` (script)

---

## Supported Games

| Game | Notes |
|---|---|
| Shuumatsu no Sugoshikata | `.pft` key file required for `.dsk` decryption |

---

## Overview

The Abogado SDK engine stores game resources inside encrypted `.dsk` archives. A companion `.pft` key file is required to decrypt them. Dialogue scripts are stored in `.scf` (Script Command File) format, which can be exported to JSON/TXT for translation and rebuilt afterward.

This tab has **two independent sections**:
- **Archive Extraction** — decrypt and unpack `.dsk` archives using the `.pft` key
- **Script Translation** — parse, translate, and rebuild `.scf` script files

---

## Section A — DSK Archive (Unpack)

### Step 1 — Select the Key File (.pft)

1. Under **ARCHIVE**, click **Browse** next to **KEY FILE (.PFT)** and select the `.pft` file from your game directory.

>  **The `.pft` key file is mandatory.** Without it the `.dsk` archive cannot be decrypted. Do not skip this step.

### Step 2 — Select the Archive (.dsk)

1. Click **Browse** next to **INPUT ARCHIVE (.DSK)** and select the `.dsk` file you want to unpack.
2. Click **Browse** next to **OUTPUT FOLDER** and choose an empty folder where the extracted files will be saved.

### Step 3 — Unpack

1. Click **Unpack DSK**.
2. The tool will decrypt and extract all files from the archive into the output folder.

---

## Section B — Script Translation (.scf ↔ JSON/TXT)

### Step 4 — Load the Script File

1. Click **Browse** next to **SCRIPT FILE (.SCF)** and select the `.scf` script file extracted from the archive.

### Step 5 — Parse to JSON/TXT

1. Click **Parse SCF**.
2. A JSON or TXT file is generated next to the selected `.scf` file containing all extractable dialogue strings.
3. Open the file in any text editor, translate the strings, and save.

### Step 6 — Inject Translation

1. Click **Browse** next to **TRANSLATION FILE (JSON/TXT)** and select your translated JSON or TXT file.
2. Click **Rebuild SCF**.
3. A new `.scf` file is produced with your translations injected.
4. Replace the original `.scf` inside your unpacked game data with the rebuilt file and test.

---

## Full Workflow Summary

```
ARCHIVE WORKFLOW:
① Browse KEY FILE (.pft) — mandatory for decryption
② Browse INPUT ARCHIVE (.dsk)
③ Browse OUTPUT FOLDER
④ Click Unpack DSK → files extracted to output folder

SCRIPT WORKFLOW:
① Browse SCRIPT FILE (.scf) from the extracted folder
② Click Parse SCF → generates JSON/TXT translation file
③ Translate the strings in the JSON/TXT
④ Browse TRANSLATION FILE (JSON/TXT)
⑤ Click Rebuild SCF → new .scf with translations injected
⑥ Replace original .scf in game data → test
```

