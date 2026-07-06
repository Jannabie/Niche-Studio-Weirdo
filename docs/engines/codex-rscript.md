# codeX RScript Engine

**Tab Name:** codeX RScript  
**Powered by:** codeX RScript Tools (bundled)  
**File Formats:** `.gsc` (compiled bytecode scripts)

---

## Supported Games

| Game | Developer | Notes |
|---|---|---|
| Forest | Liar-soft | Shift-JIS encoding |
| Other codeX RScript titles | Various | Check encoding before processing |

---

## Overview

The codeX RScript engine stores dialogue and game logic in compiled `.gsc` bytecode files. This tab provides tools to export the text from `.gsc` files to JSON for translation, verify the export/import roundtrip is lossless, and inject translated text back into `.gsc` files.

The tool supports both **single file** and **batch folder** processing modes.

---

## Step 1 — Select Encoding

1. At the top of the tab, choose the **Encoding** from the selector dropdown.
2. The default is **Shift-JIS** — use this for most Japanese VN titles.

> ⚠️ **Setting the wrong encoding will produce garbled text.** Always verify encoding before processing. If you are unsure, try Shift-JIS first.

---

## Step 2 — Select Input Mode

Choose whether to process a single file or a batch of files:

- **Single File Mode** — click **Browse** next to **INPUT .GSC FILE** and select one `.gsc` file.
- **Batch Folder Mode** — click **Browse** next to **INPUT FOLDER** and select the folder containing all your `.gsc` files.

---

## Step 3 — Export Text → JSON

1. Click **Export → JSON**.
2. A `.json` file is generated for each processed `.gsc` file, saved next to the originals.
3. Open the JSON files in a text editor and translate the dialogue strings.

---

## Step 4 — (Optional) Verify Roundtrip

Before injecting your translation, you can verify the export/import pipeline is working correctly:

1. Click **Verify Roundtrip**.
2. The tool re-imports the unmodified JSON and checks the output `.gsc` is byte-for-byte identical to the original.

> 💡 **Always run Verify Roundtrip on at least one file** before committing to a full translation. If it fails, there may be an encoding mismatch or unsupported opcode.

---

## Step 5 — Import JSON → GSC

1. After translating, click **Import JSON → GSC**.
2. The tool injects the translated strings from the JSON files back into new `.gsc` files.
3. Replace the original `.gsc` files in your game directory with the new ones and test.

---

## Full Workflow Summary

```
① Select correct Encoding (default: Shift-JIS)
② Select input: Single File (.gsc) OR Batch Folder
③ Click Export → JSON → translate the generated JSON files
④ (Optional) Click Verify Roundtrip → confirm lossless pipeline
⑤ Click Import JSON → GSC → new .gsc files with translations
⑥ Replace original .gsc files in game directory → test
```

> ⚠️ **Do not change the JSON key/structure.** Only edit the string values. Changing keys or removing entries will cause import errors.
