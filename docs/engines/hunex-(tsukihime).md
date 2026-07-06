# HuneX Engine (Tsukihime Remake)

**Tab Name:** HuneX (Tsukihime)  
**Powered by:** HuneX Tools — Tsukihime Remake variant (bundled)  
**File Formats:** `script_text.mrg` (binary script archive)

---

## Supported Games

| Game | Version | Notes |
|---|---|---|
| Tsukihime -A piece of blue glass moon- | **Japanese version only** | English version is NOT supported |

> ⚠️ **Only the Japanese version of Tsukihime Remake is supported.** The English/localized release uses a different format that is incompatible with this tool.

---

## Overview

The HuneX engine (Tsukihime Remake variant) stores all dialogue and scene data in a single binary archive: `script_text.mrg`. This file contains multiple scene scripts packed together. The tool extracts them to editable `.TXT` files (organized into scene folders) and repacks them after translation.

> ⚠️ **CRITICAL — Line Count Constraint:** The repacked `.MRG` must have **exactly the same number of lines** as the original in every scene file. Adding or removing lines will corrupt the script and crash the game. You may only replace existing lines — never add or delete them.

---

## Step 1 — Select script_text.mrg

1. Click **Browse** next to **INPUT FILE** and select `script_text.mrg` from your game's data directory.

---

## Step 2 — Extract → .TXT Scene Files

1. Click **Extract → .TXT**.
2. The tool unpacks all scenes from `script_text.mrg` into a folder structure organized by scene, saved next to the `.mrg` file.
3. Open the `.TXT` files in your text editor. Each file represents one scene — translate the dialogue lines.

> ⚠️ **Do not add or remove lines.** Each line maps to a fixed address inside the binary. The line count per file must remain identical to the original. Only change the text content of existing lines.

---

## Step 3 — Translate the Scene Files

1. Work through the extracted `.TXT` scene files and replace the Japanese text with your translations.
2. Keep every existing line — even blank or formatting lines — exactly in place.

> 💡 **Line position = game pointer.** The game reads each line by its index. Shifting any line breaks all subsequent dialogue in that scene.

---

## Step 4 — Repack → .MRG

1. Click **Repack → .MRG**.
2. The tool reads all the `.TXT` files from the scene folders and packs them back into a new `script_text.mrg`.
3. Replace the original `script_text.mrg` in your game's data directory with the new file and test.

---

## Full Workflow Summary

```
① Browse script_text.mrg (Japanese version only!)
② Click Extract → .TXT → scene folders with editable .TXT files appear
③ Translate each .TXT file — NEVER add or remove lines
④ Click Repack → .MRG → new script_text.mrg generated
⑤ Replace original script_text.mrg in game directory → test
```

> ⚠️ **Line count must stay exactly the same.** This is the single most common cause of crashes when working with this engine — always double-check before repacking.
