# Luca System (Key / Visual Art's)

**Tab Name:** Luca System  
**Powered by:** [LuckSystem (Yoremi Fork)](https://github.com/wetor/LuckSystem)  
**File Formats:** `SCRIPT.PAK`, `.CZ0` / `.CZ1` / `.CZ2` / `.CZ3` (images)

---

## Supported Games

| Profile | Game |
|---|---|
| LOOPERS | LOOPERS (Steam) |
| LB_EN | Little Busters! English Edition (Steam) |
| SP | Summer Pockets (Nintendo Switch) |
| Cartagra HD | Cartagra HD (Japanese version only) |
| KANON | KANON (Steam) |
| HARMONIA | HARMONIA (Steam) |
| LUNARiA | LUNARiA (Steam) |
| AIR | AIR (Steam) |
| Planetarian SG | Planetarian SG (Steam) |

>  **Cartagra HD (English translation version) is NOT supported.** The English publisher modified the bytecode format in a way that is incompatible with LuckSystem.

---

## Overview

The Luca System (also called the "Prototype Engine") is used by Key/Visual Art's games. It stores dialogue and game logic inside `SCRIPT.PAK` files, and UI/CG images in `.CZ` format (proprietary compressed image).

This tab has **two independent sections**:
- **Script Translation** — decompile / recompile `SCRIPT.PAK`
- **Image Translation** — convert `.CZ` images to `.PNG` and back

> 💡 The **Game Profile** only matters for Script operations. For CZ image conversion, you can ignore it entirely.

---

## Section A — Script Translation (SCRIPT.PAK ↔ Text Files)

### Step 1 — Select Game Profile

Choose the correct game from the **TARGET GAME / PROFILE** dropdown. This sets the bytecode format and opcode table for the decompiler. Getting this wrong will produce garbled output.

### Step 2 — Decompile SCRIPT.PAK

1. Under **DECOMPILE**, click `...` next to **INPUT SCRIPT.PAK** and select the `SCRIPT.PAK` file from your game data directory.
2. Click **Decompile SCRIPT.PAK**.
3. Output is automatically saved to a `Script_Decompiled` folder next to your `SCRIPT.PAK` file — no need to specify an output location.

### Step 3 — Translate

Open the text files in the `Script_Decompiled` folder. Translate the dialogue strings.

>  **FULLWIDTH / ZENKAKU character limits apply.** The Luca System engine has fixed byte-length limits per line. Use fullwidth (Zenkaku) characters for Latin text to stay within bounds. Exceeding these limits will cause text overflow or crash.

### Step 4 — Import Translated Text → New SCRIPT.PAK

1. Under **IMPORT**, click `...` next to **ORIGINAL SCRIPT.PAK** and point to the **unmodified** original `SCRIPT.PAK`.
2. Click `...` next to **MODIFIED TEXT FOLDER** and select the `Script_Decompiled` folder containing your translated files.
3. Click **Import → New SCRIPT.PAK**.
4. Output is automatically saved as `SCRIPT_NEW.PAK` next to your original.
5. Replace the original `SCRIPT.PAK` in your game directory with `SCRIPT_NEW.PAK` (renamed) and test.

---

## Section B — Image Translation (CZ Image ↔ PNG)

The Luca System stores images in `.CZ` format (variants: `.CZ0`, `.CZ1`, `.CZ2`, `.CZ3`). These must be converted to `.PNG` for editing in standard image editors, then converted back.

### Export CZ → PNG

1. Under **EXPORT**, click `...` next to **INPUT CZ FILE** and select any `.CZ0`/`.CZ1`/`.CZ2`/`.CZ3` image file.
2. Click **Export CZ → PNG**.
3. The `.png` file is saved automatically next to the original CZ file.
4. Edit the `.png` in Photoshop, GIMP, or any image editor.

### Import PNG → CZ

1. Under **IMPORT**, click `...` next to **ORIGINAL CZ FILE** — this is the **original** unmodified CZ file (needed to read the format metadata).
2. Click `...` next to **MODIFIED .PNG** — select your edited PNG.
3. Click **Import PNG → CZ**.
4. Output is saved as `filename_imported.czX` next to the original.
5. Rename and replace the original CZ file in your game data directory.

---

## Full Workflow Summary

```
SCRIPT WORKFLOW:
① Select correct Game Profile
② Decompile SCRIPT.PAK → Script_Decompiled folder
③ Translate the extracted text files (mind FULLWIDTH limits!)
④ Import translated folder + original SCRIPT.PAK → SCRIPT_NEW.PAK
⑤ Replace SCRIPT.PAK in game directory → test

IMAGE WORKFLOW:
① Export CZ → PNG (any .CZ0/.CZ1/.CZ2/.CZ3)
② Edit the PNG in your image editor
③ Import PNG + original CZ file → _imported.czX
④ Replace the original CZ file in game directory → test
```
