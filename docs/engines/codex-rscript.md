# codeX RScript

> Export and import bytecode `.gsc` scripts to/from JSON with roundtrip validation.

**Example games:** Various titles using the codeX RScript engine

---

## Tools Available

### GSC ↔ JSON Converter

| Button | Action |
|---|---|
| **Export GSC → JSON** | Decompile `.gsc` bytecode into an editable JSON file |
| **Import JSON → GSC** | Recompile translated JSON back into `.gsc` bytecode |
| **Verify Roundtrip** | Check that export → import produces a byte-identical result |

---

## Settings

### Encoding
| Option | Use when |
|---|---|
| **Shift-JIS** (default) | Japanese original scripts |
| **UTF-8** | Scripts that already use Unicode encoding |

### Operation Mode
| Option | Description |
|---|---|
| **Single File (.gsc)** | Process one `.gsc` file at a time |
| **Batch Folder** | Process all `.gsc` files in a folder at once |

---

## How to Use

**Step 1 — Export:**
1. Set Encoding and Mode as appropriate
2. Browse → select your `.gsc` file (or folder in Batch mode)
3. Click **Export GSC → JSON** → save the output `.json`

**Step 2 — Translate:**
- Open the `.json` and translate the text values
- Do not change keys or structural elements

**Step 3 — Verify (recommended):**
1. Select the original `.gsc` and the exported JSON
2. Click **Verify Roundtrip** — if it passes, the export was lossless and reimport will be safe

**Step 4 — Import:**
1. Select your translated `.json`
2. Click **Import JSON → GSC** → save the new `.gsc`
3. Replace the original in the game directory

> **Tip:** Always run **Verify Roundtrip** on the original (untranslated) export before translating. If verification fails, the script format may not be fully supported.
