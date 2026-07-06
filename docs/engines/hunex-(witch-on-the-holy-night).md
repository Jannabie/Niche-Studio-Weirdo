# HuneX Engine (Witch on the Holy Night Remastered)

**Tab Name:** HuneX (Mahoyo)  
**Powered by:** HuneX Tools — Mahoyo Remastered variant (bundled)  
**File Formats:** `.hfa` (main archive), `.ctd` (scripts), `.cbg` (compressed background images), `.mzp` (sprites / UI images)

---

## Supported Games

| Game | Notes |
|---|---|
| Mahoutsukai no Yoru (Witch on the Holy Night) Remastered | All asset types handled by this tab |

---

## Overview

The HuneX engine (Mahoyo variant) packs all game resources into a single `.hfa` container archive. Inside it you will find:
- **`.ctd`** — dialogue/scene scripts
- **`.cbg`** — compressed background images
- **`.mzp`** — sprite and UI images

The workflow is: **Unpack HFA → work on individual files → Repack HFA**.

---

## Step 1 — Select and Unpack the HFA Archive

1. Click **Browse** next to **INPUT .HFA FILE** and select the `.hfa` archive from your game directory.
2. Click **Unpack HFA**.
3. All files are extracted into a folder next to the `.hfa` file.

---

## Section A — CTD Script (Extract & Repack)

### Step 2A — Extract .CTD

1. Under **CTD SCRIPT**, click **Browse** and select a `.ctd` script file from the unpacked folder.
2. Click **Extract CTD**.
3. An editable text or JSON file is generated — open it, translate the dialogue, and save.

### Step 3A — Repack .CTD

1. Click **Repack CTD**.
2. The tool writes your translated text back into a new `.ctd` file.
3. Place the new `.ctd` back into the extracted folder (replacing the original).

---

## Section B — CBG Background Images (Extract & Repack)

### Step 2B — Extract .CBG → Editable Format

1. Under **CBG IMAGE**, click **Browse** and select a `.cbg` file from the unpacked folder.
2. Click **Extract CBG**.
3. The image is decompressed and saved as an editable format (e.g. `.png` or `.bmp`).
4. Edit the image in your preferred image editor.

### Step 3B — Repack .CBG

1. Click **Repack CBG**.
2. The tool recompresses your edited image back to `.cbg` format.
3. Place the new `.cbg` back into the extracted folder.

---

## Section C — MZP Sprites / UI (Extract & Repack)

### Step 2C — Extract .MZP → Editable Format

1. Under **MZP IMAGE**, click **Browse** and select a `.mzp` file from the unpacked folder.
2. Click **Extract MZP**.
3. The sprite or UI image is extracted to an editable format.
4. Edit as needed in your image editor.

### Step 3C — Repack .MZP

1. Click **Repack MZP**.
2. The tool repacks your edited image back into `.mzp` format.
3. Place the new `.mzp` back into the extracted folder.

---

## Step 4 — Repack HFA Archive

Once all your translated/edited files are in place in the extracted folder:

1. Click **Repack HFA**.
2. The tool assembles all files back into a new `.hfa` archive.
3. Replace the original `.hfa` in your game directory with the new file and test.

> ⚠️ **Make sure all individual file repacks (CTD / CBG / MZP) are done before repacking the HFA.** Any file still in intermediate state will not be included correctly.

---

## Full Workflow Summary

```
① Browse .HFA FILE → Click Unpack HFA → files extracted to folder

SCRIPT:
② Browse .ctd → Click Extract CTD → translate → Click Repack CTD
③ Replace .ctd in extracted folder

IMAGES (as needed):
④ Browse .cbg → Click Extract CBG → edit → Click Repack CBG → replace in folder
⑤ Browse .mzp → Click Extract MZP → edit → Click Repack MZP → replace in folder

FINAL:
⑥ Click Repack HFA → new .hfa archive generated
⑦ Replace original .hfa in game directory → test
```

> 💡 **Back up the original `.hfa`** before you start. Repacking is non-destructive as long as you keep a copy of the original extracted folder.
