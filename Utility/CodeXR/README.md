# codeX RScript Script Extractor & Repacker

A tool for reading, editing, and repacking `.gsc` script files from the Visual Novel **Forest** (Liar Soft), built on the **codeX RScript** engine.

---
| Screenshot |
|:---:|
| ![Proof](https://i.imgur.com/RtR8Go4.png) |
| *Translation running in-game* |

---
## Game File Structure
| File | Content |
|---|---|
| `scr.xfl` | LB archive containing 103 `.gsc` files (all game scripts) |
| `grpo.xfl`, `grpo_bg.xfl`, etc. | LB archives containing graphic assets (`.wcg`, `.lwg`) |
| `grps.xfl` | LB archive containing UI, dialogue images, choice images |

`.gsc` files containing dialogue are usually the larger ones (±50–400 KB) — such as `2100.gsc`, `2300.gsc`, `2500.gsc`, `2600.gsc`, etc. Smaller ones (< 5 KB) only contain engine logic/initialization, safe to skip.

---
## .gsc Format
The `.gsc` file is **compiled bytecode** from codeX RScript:
```
[Header 28 bytes]
[Code / Bytecode]
[Offset Table]   ← pointer to each string
[String Table]   ← null-terminated strings (variable names & dialogue text)
[Section C]
[Section D]
[Extra trailing]
```
Header: 7 × `uint32` little-endian:
| Offset | Field |
|---|---|
| +0x00 | Total size of all sections |
| +0x04 | Header size (always 28) |
| +0x08 | Code size |
| +0x0C | Offset table size |
| +0x10 | String table size |
| +0x14 | Section C size |
| +0x18 | Section D size |

---
## How to Use

### 1. View file contents
```bash
python gsc_tool.py info 2500.gsc
python gsc_tool.py list *.gsc
```

### 2. Export strings to JSON
```bash
python gsc_tool.py export 2500.gsc -o 2500.json
```

### 3. Edit the translation
Open the JSON, fill in the `"translated"` field — don't change anything else:
```json
{
  "index": 644,
  "offset": 41002,
  "original": "^ckThe time......t-twelve......",
  "translated": "^ckIt's time......t-twelve......"
}
```
>  Do not touch `"original"`, `"index"`, or `"offset"`. Characters like `^ck` are engine **control codes** — they must be copied over as-is.

### 4. Repack into .gsc
```bash
python gsc_tool.py import 2500.gsc 2500.json -o 2500_translated.gsc
```

### 5. Batch processing
```bash
# Export all into the json/ folder
python gsc_tool.py export-all *.gsc -d json/

# Import all once translation is done
python gsc_tool.py import-all *.gsc -d json/ -o repacked/
```

### 6. Verify roundtrip
```bash
python gsc_tool.py verify *.gsc
```
All 11 sample files have been verified as **100% identical** after the roundtrip.

---
## All Commands
| Command | Function |
|---|---|
| `info <file>` | Detailed info + string list |
| `info -v <file>` | + bytecode hex dump |
| `list <files...>` | Summary of multiple files at once |
| `export <file> -o out.json` | Export strings to JSON |
| `import <file> <json> -o out.gsc` | Import JSON → repack .gsc |
| `repack <file> -o out.gsc` | Rebuild without editing |
| `verify <files...>` | Check roundtrip is identical |
| `export-all <files...> -d dir/` | Export all into a folder |
| `import-all <files...> -d dir/ -o dir/` | Import & repack all |

---
## Notes
- Default encoding: **Shift-JIS**. If your translation uses non-ASCII characters (e.g. `é`), add the `--encoding utf-8` flag during import — but make sure the engine supports it first.
- The string table in a large `.gsc` file stores **both variable names and dialogue text** — both exist in the same place.
- Small `.gsc` files (< 5 KB) contain no dialogue and are safe to ignore.
