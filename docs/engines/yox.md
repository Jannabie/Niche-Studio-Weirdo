# YOX Engine

**Tab Name:** YOX  
**Powered by:** YOX Tools (bundled)  
**File Formats:** `.dat` (encrypted archive), `.dec` (intermediate decrypted), `manifest.json` (file index)

---

## Supported Games

| Game | Notes |
|---|---|
| Musicus! | Strict 4-step pipeline required |

---

## Overview

The YOX engine uses a strict **4-step sequential pipeline** for translation. Steps must be performed in exact order — skipping or reordering any step will break the pipeline and require starting over from Step 1.

The pipeline produces intermediate files (`.dec` and `manifest.json`) that are used across steps. These files must remain in their generated locations.

>  **Do not move or rename `manifest.json` or `.dec` files** between steps. They are referenced by path in subsequent operations. Moving them will cause Step 3 and Step 4 to fail.

---

## Step 1 — Unpack DAT

1. Click **Browse** next to **INPUT .DAT FILE** and select the encrypted `.dat` archive from your game directory.
2. Click **Unpack DAT**.
3. The tool decrypts the archive and generates:
   - One or more `.dec` intermediate files
   - A `manifest.json` file index — saved next to the `.dat`

>  **`manifest.json` is the map of the archive.** It records the file list and offsets needed for all subsequent steps. Keep it where the tool saves it.

---

## Step 2 — Export JSON

1. Click **Export JSON**.
2. The tool reads the `.dec` files and extracts all dialogue and text strings into a `.json` translation file.
3. Open the JSON file in a text editor, translate the strings, and save.

---

## Step 3 — Import JSON

1. After translating, click **Import JSON**.
2. The tool injects your translated strings from the JSON back into the `.dec` intermediate files.

>  **Do not modify the `.dec` files manually** between Step 2 and Step 3. Only the JSON translation file should change. Manual edits to `.dec` files will corrupt the injection.

---

## Step 4 — Repack DAT

1. Click **Repack DAT**.
2. The tool reads the modified `.dec` files and `manifest.json` to assemble the final encrypted `.dat` archive.
3. Replace the original `.dat` in your game directory with the new file and test.

---

## Full Workflow Summary

```
① Browse INPUT .DAT FILE → Click Unpack DAT
   → Generates: .dec files + manifest.json

② Click Export JSON
   → Generates: translation .json file
   → Translate the strings in the JSON

③ Click Import JSON
   → Injects translations back into .dec files

④ Click Repack DAT
   → Generates: final translated .dat archive
   → Replace original .dat in game directory → test
```

>  **Steps must be performed in order: 1 → 2 → 3 → 4.** The output of each step is required input for the next. Never skip steps or run them out of sequence.
