# Leaf / KCAP Engine

**Tab Name:** Leaf  
**Powered by:** Leaf KCAP Tools (bundled)  
**File Formats:** `.pak` (KCAP archives)

---

## Supported Games

| Game | Notes |
|---|---|
| White Album 2 | Shift-JIS filenames inside `.pak` |

---

## Overview

The Leaf engine uses KCAP-format `.pak` archives to store all game resources. Filenames inside the archive are encoded in Shift-JIS. The tool correctly handles Shift-JIS filename preservation during extraction and repacking.

> ⚠️ **CRITICAL — Clean Workspace Required Before Repacking:** Any extra files present in the workspace folder at repack time will be bundled into the new archive. Always start with a clean, empty workspace folder — or verify that it contains only the files you intend to include.

---

## Step 1 — Select Workspace Directory

1. Click **Browse** next to **WORKSPACE DIRECTORY** and select an **empty** folder to use as your working area.

> ⚠️ **The workspace must be empty before repacking.** If it contains leftover files from a previous session, those files will be accidentally included in your new archive.

---

## Step 2 — Select the PAK Archive

1. Click **Browse** next to **INPUT .PAK FILE** and select the `.pak` file from your game directory.

---

## Step 3 — Unpack .PAK

1. Click **Unpack .PAK**.
2. All files are extracted into the workspace directory, preserving Shift-JIS filenames correctly.
3. Open the extracted files, make your translation edits, and save.

> 💡 **Shift-JIS filenames** will display correctly if your system has Japanese locale set. If filenames appear as question marks or boxes, set your system locale to Japanese (or use a Unicode-aware file manager) before working with them.

---

## Step 4 — Repack to .PAK

1. Ensure the workspace directory contains **only** the files you want in the final archive.
2. Click **Repack to .PAK**.
3. A new `.pak` archive is generated from the workspace contents.
4. Replace the original `.pak` in your game directory with the new file and test.

---

## Full Workflow Summary

```
① Browse WORKSPACE DIRECTORY → choose an empty folder
② Browse INPUT .PAK FILE
③ Click Unpack .PAK → files extracted to workspace (Shift-JIS names preserved)
④ Translate/edit the extracted files
⑤ Verify workspace contains ONLY the intended files
⑥ Click Repack to .PAK → new archive generated
⑦ Replace original .pak in game directory → test
```

> ⚠️ **Extra files in the workspace = extra files in the archive.** This is the most common mistake with this engine — always double-check your workspace contents before repacking.
