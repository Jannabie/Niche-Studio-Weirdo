# EntisGLS Engine

**Tab Name:** EntisGLS  
**Powered by:** arc_unpacker (bundled), noa32c (bundled), custom C# SRCXML parser  
**File Formats:** `.noa` (archives), `.srcxml` (scripts), `.eri` (images → auto-converted to `.png`)

---

## Supported Games

| Game | Notes |
|---|---|
| Bakaple | Confirmed working |

---

## Overview

The EntisGLS engine stores game data inside `.noa` archive files. Scripts are stored as `.srcxml` files — a structured XML-like format containing dialogue and event data. Image assets use the `.eri` format, which the tool automatically converts to standard `.png` during extraction so translators can view and edit UI graphics without needing a separate converter.

> **IMPORTANT — One-Way Operation:**  
> Repacked `.noa` files (output from the **Repack** step) **cannot be unpacked again** using this tool. The internal format of a repacked `.noa` differs from the original game file. **Always keep a backup of the original `.noa` file before making any modifications.**

---

## Step 1 — Unpack .NOA

1. Click **Browse** (or `...`) next to **INPUT .NOA FILE** and select the `.noa` archive from your game directory.
2. Click **Unpack .NOA → Folder**.
3. The archive is extracted to a folder next to the `.noa` file.
   - `.srcxml` script files are extracted as-is
   - `.eri` image files are automatically decoded to `.png`

---

## Step 2 — Parse SRCXML → TXT

1. Click **Browse** next to **SRCXML FOLDER** and select the folder containing the extracted `.srcxml` files.
2. Click **Browse** next to **TXT TRANSLATION FOLDER** and select (or create) an empty folder where the output `.txt` files will be saved.
3. Click **Parse (SRCXML → TXT)**.
4. One `.txt` file per `.srcxml` is generated in the translation folder. Each file contains the extracted dialogue lines, ready for translation.

---

## Step 3 — Translate

Open the `.txt` files in any text editor (Notepad++, VS Code, etc.) and fill in your translations. The format marks original lines clearly — only edit the translation slots.

---

## Step 4 — Inject TXT → SRCXML

1. Ensure the **SRCXML FOLDER** field still points to the extracted scripts from Step 1.
2. Ensure the **TXT TRANSLATION FOLDER** field points to your completed `.txt` translation files from Step 3.
3. Click **Inject (TXT → SRCXML)**.
4. The translated text is written back into the `.srcxml` files in-place inside the SRCXML folder.

---

## Step 5 — Repack Folder → .NOA

1. Click **Browse** next to **SOURCE FOLDER (TRANSLATED SRCXML)** and select the folder containing the modified `.srcxml` files (same folder from Step 1).
2. Click **Repack → .NOA**.
3. A new `.noa` archive is generated. Replace the original in your game directory and test.

> **Reminder:** The repacked `.noa` cannot be unpacked again — always keep the original backed up.

---

## Full Workflow Summary

```
① Browse INPUT .NOA FILE → Click Unpack .NOA → Folder
   └─ .srcxml scripts extracted, .eri images auto-converted to .png

② Browse SRCXML FOLDER + TXT TRANSLATION FOLDER
   → Click Parse (SRCXML → TXT) → .txt files generated per script

③ Translate the .txt files

④ Click Inject (TXT → SRCXML) → translations written back into .srcxml

⑤ Browse SOURCE FOLDER (TRANSLATED SRCXML)
   → Click Repack → .NOA → new archive generated

⑥ Replace original .noa in game directory → test
```
