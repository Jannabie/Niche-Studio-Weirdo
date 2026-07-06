# YU-RIS Engine (RxYuris)

**Tab Name:** YU-RIS  
**Powered by:**
- [RxYuris / YurisTools](https://github.com/) — YPF archive tools & YSTB XOR key tools (bundled in `Utility/RxYurisBin/`)
- [ypf-repacker](https://github.com/) — YPF archive repacker (bundled in `Utility/ypf-repacker/`)
- [VNTextPatch](https://github.com/arcusmaximus/VNTranslationTools) by arcusmaximus — YBN script extract/inject (bundled in `Utility/VNTextPatch/`)

**File Formats:** `.ypf` (archive), `.ybn` (compiled script), `.json` (extracted text)

---

## Supported Games (Examples)

- Maggot Baits (clockup)
- Erewhon (clockup)
- ef – a fairy tale of the two (minori) *(YPF archive layer)*
- Fraternité (Liar-soft) *(YPF archive layer)*
- Other YU-RIS engine titles

---

## Overview

The YU-RIS engine has two independent layers:
1. **YPF Archive Layer** — game resources (graphics, audio, scripts) are packed inside `.ypf` archives
2. **YBN Script Layer** — dialogue and game logic are stored in compiled `.ybn` binary files inside the archive

This tab handles both layers independently.

---

## Section A — YPF Archive (Extract & Repack)

### Extract .YPF → Folder

1. Under **YPF ARCHIVE — EXTRACT & REPACK**, click **Browse** next to **EXTRACT: SELECT .YPF FILE**.
2. Select the `.ypf` archive from your game directory.
3. Click **Extract YPF**. The contents are extracted into a folder next to the `.ypf` file.

> 📖 **IMPORTANT NOTE:** Many YU-RIS games prioritize loading **loose files** over the packed archive. You might not need to repack at all — just put the extracted folder in the game directory and the game will load from it directly. Try this first before repacking!

### Repack Folder → .YPF

1. Under **REPACK: SELECT FOLDER TO PACK**, click **Browse** and select the folder you want to pack.
2. *(Optional)* Set the **Engine Version** number (default: `0.479`). Check your game's `EngineVersion` or leave the default for most games.
3. *(Optional)* Check **Use CRC32 Hash** if the repacked YPF doesn't load in-game (fixes duplicate `[]` errors).
4. Click **Repack to .YPF**.

---

## Section B — YBN Script (Decrypt & Translate)

`.ybn` files are binary-compiled scripts that contain dialogue and event logic. They need to be decrypted and extracted to JSON for translation, then injected back.

> 📖 **IMPORTANT NOTE:** Not all `.ybn` files contain dialogue. Many files are pure system code/logic. If you have 300+ `.ybn` files but only ~150 produce JSON output, **that is completely normal**. Only files with dialogue strings will generate JSON.

### Step 1 — Decrypt & Extract Text

1. Under **YBN SCRIPT — ONE-CLICK WORKFLOW**, click **Browse** and select the **folder** containing all your `.ybn` files (typically the `script` subfolder inside your extracted YPF folder).
2. Click **Decrypt & Extract Text**.
3. The tool runs VNTextPatch on each `.ybn` file one by one.
4. JSON files are saved inside a `script_txt` subfolder within your selected folder.
5. **Only `.ybn` files that contain dialogue** will produce a corresponding `.json` file. This is expected behavior.

### Step 2 — Translate

Open the `.json` files in the `script_extracted` folder with a text editor. Edit the `"message"` fields with your translations.

**JSON format example:**
```json
[
  {
    "message": "The script of the game" <- Replace with your translation.
  }
]
```

### Step 3 — Insert Text & Encrypt

1. Click **Browse** (same folder as before — the folder containing your `.ybn` files).
2. Click **Insert Text & Encrypt**.
3. The tool:
   - Copies all original `.ybn` files to a new `script_patched` subfolder
   - Injects your translated JSON into each matching `.ybn` in that folder
4. Your translated `.ybn` files are now in `script_patched`.

### Step 4 — Repack & Test

1. Replace the original `.ybn` files in your extracted YPF folder with the ones from `script_patched`.
2. Either use the **loose file method** (copy the folder directly into the game directory) or repack using Section A above.
3. Launch the game and test.

---

## Full Workflow Summary

```
① Extract YPF archive → extracted folder
② Open the "script" subfolder (or wherever .ybn files are)
③ Decrypt & Extract Text → JSON files appear in script_txt/
④ Translate the "message" fields in the JSON files
⑤ Insert Text & Encrypt → translated .ybn files appear in script_patched/
⑥ Replace original .ybn files with the ones from script_patched/
⑦ Copy the folder to game directory (loose files) OR repack to .YPF
⑧ Launch game and test
```

>  **Tip:** Try the loose-file method (Step 7, no repack) first — it's faster and easier to iterate on.
