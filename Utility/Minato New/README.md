# Minato Engine New Tools

Tools for extracting, editing, and repacking script archives from **Waga Himegimi ni Eikan o** (likely also works for other Forlos/vn_re-engine titles).

**Requirements:** Python 3.10+, no external deps.

---

## Files

| File | Purpose |
|---|---|
| `acv1_extractor.py` | Decrypts & extracts entries from a `.dat` archive |
| `acv1_repacker.py` | Repacks edited scripts back into a `.dat` |
| `wh_script_parser.py` | Parses scripts to JSON/CSV for translation |

---

## Workflow

### 1. Extract

```bash
python acv1_extractor.py script3.dat
```

Output folder: `script3_extracted/`
Each entry is saved as `<index>_<checksum>.txt` (UTF-8), plus a `manifest.json` with complete metadata.

---

### Which files are okay to edit

After extraction, the folder structure will look like this:

```
script3_extracted/
├── 0000_ad1680ebaeb5758f.txt   ← DO NOT TOUCH
├── 0001_d3587ef8e0f46d43.txt   ← DO NOT TOUCH
├── 0002_76f6ad0f25e32254.txt   ← DO NOT TOUCH
├── 0003_557de59f61f89207.txt   ← DO NOT TOUCH
├── 0004_a7f158b0cfb5261b.txt   ← DO NOT TOUCH
├── 0005_8115fa5ee29c3c4f.txt   ← DO NOT TOUCH
├── 0006_2f15b334e3063475.txt   ← DO NOT TOUCH
├── 0007_1ccfe3193516609e.txt   ← start editing from here
├── 0008_xxxxxxxxxxxx.txt       ← and so on
└── ...
```

**Only edit from index `0007` onward.** Files `0000`–`0006` are internal engine metadata — editing them can corrupt the archive or crash the game.

> The number at the start of the filename (e.g. `0007`) is the entry index. The long hex string after it (e.g. `1ccfe3193516609e`) is the checksum required by the repacker. **Do not rename these files.** The filename must remain exactly as it was when extracted.

---

### 2. Translate

Instead of editing the `.txt` files directly, it's better to use `wh_script_parser.py` to export the dialogue to CSV — much easier to work with in Google Sheets or Excel.

#### Export to CSV

```bash
python wh_script_parser.py export-csv 0007_1ccfe3193516609e.txt --out 0007_dialog.csv
```

The result will look like this:

| line_number | is_narration | speaker_internal | speaker_display | voice_id | original_text | translated_text |
|---|---|---|---|---|---|---|
| 45 | no | Foru | Foru | S035_B2_0036 | Okay, I feel better... | *(fill in here)* |
| 54 | no | Chimes | Chimes | S061_B2_0002 | I-It's okay, I'll do that. | *(fill in here)* |
| 119 | yes | | | | A tremendous roar... | *(fill in here)* |

Fill in the **`translated_text`** column. If a line hasn't been translated yet, just leave it blank — it will automatically fall back to the original text.

#### Rebuild after translating

```bash
# Step 1 — parse to JSON first (only needed once per file)
python wh_script_parser.py parse 0007_1ccfe3193516609e.txt --out 0007.json

# Step 2 — rebuild the .txt with the filled-in translation
python wh_script_parser.py import 0007.json --csv 0007_dialog.csv --out 0007_1ccfe3193516609e.txt
```

> Pay attention to the output filename in Step 2: it must be exactly `0007_1ccfe3193516609e.txt` — identical to the original filename when extracted. If it differs, the repacker will skip it and use the original text instead.

#### Other commands

```bash
# Preview dialogue in the terminal without writing a file
python wh_script_parser.py show-dialog 0007_1ccfe3193516609e.txt

# View a summary of line count, speakers, and command types
python wh_script_parser.py stats 0007_1ccfe3193516609e.txt

# Export the full script structure to JSON
python wh_script_parser.py parse 0007_1ccfe3193516609e.txt --out 0007.json

# Process all .txt files at once
python wh_script_parser.py parse-all script3_extracted/ --out-dir json_out/
```

---

### 3. Repack

```bash
python acv1_repacker.py script3.dat script3_extracted --out script3_patched.dat
```

Done. Replace the original `.dat` file with `script3_patched.dat`.

> Make sure the rebuilt `.txt` files are already in the extracted folder with the correct names before running this.

---

## Proof of Concept

| Screenshot |
|:---:|
| ![Translation working in-game](https://i.imgur.com/3c1dV98.jpeg) |
| *Translation working in-game* |

---

## Quick Rules for Translators

| | Detail |
|---|---|
| Edit `.txt` files from index `0007` onward | Those contain the actual script |
| Don't rename files | Names must stay the same, e.g. `0007_1ccfe3193516609e.txt` |
| Don't edit `0000`–`0006` | Engine metadata, not dialogue |
| Don't edit `.bin` files | Raw bytes, fallback only |
| Don't change the encoding manually | The repacker already handles UTF-8 → CP932 automatically |

---

## Options

### Extractor
```
--out <dir>           Output directory (default: <archive>_extracted)
--master-key 0x...    ACV1 master key (default: 0x8B6A4E5F)
--script-key 0x...    Game script key (default: 0x3793B711)
--no-raw              Skip .bin files
--no-text             Skip .txt files
```

### Repacker
```
--out <path>              Output archive path (required)
--master-key 0x...        ACV1 master key (default: 0x8B6A4E5F)
--script-key 0x...        Game script key (default: 0x3793B711)
--text-encoding cp932     .txt encoding (default: cp932, do not change)
--level 0-9               zlib compression level (default: 9)
```

### Parser
```
parse <file> [--out <json>]
parse-all <folder> [--out-dir <folder>]
export-csv <file> [--out <csv>]
import <json> [--csv <csv>] [--out <txt>]
show-dialog <file>
stats <file>
```

---

## Archive Format (ACV1)

```
[4 bytes]  Magic: "ACV1"
[4 bytes]  Entry count XOR master_key
[N × 21 bytes]  Entry table (encrypted)
[...]      Packed payloads
```

Each entry (21 bytes):
```
[8]  Checksum (u64)
[1]  Flags XOR (checksum & 0xFF)
[4]  File offset XOR checksum32 XOR master_key
[4]  Packed size XOR checksum32
[4]  Unpacked size XOR checksum32
```

Decoding per entry: `blob → XOR(checksum32) → XOR(script_key) → zlib.decompress → CP932`
Encoding: the reverse.

---

## Notes

- A **different compressed size** after repacking is normal — zlib output is inherently non-deterministic. What matters is that the decompressed content is correct.
- **Encoding**: The original script is CP932. The extractor saves it as UTF-8 for easier editing, and the repacker automatically encodes it back to CP932.
- **Resource archives** with original filenames are not handled here. This tool is script-only.

---

## Known Keys — Waga Himegimi ni Eikan o

| Key | Value |
|---|---|
| Master key | `0x8B6A4E5F` |
| Script key | `0x3793B711` |
